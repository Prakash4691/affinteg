using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using CreateLeadOpportunityKycRequestConstant = custom_api_plugin.Constant.CreateLeadOpportunityKYCRequestValues;

namespace custom_api_plugin
{
    internal static class DataverseEntityHelper
    {
        private static readonly IDictionary<string, string> CurrencyAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "usd", "USD" },
                { "us dollar", "USD" },
                { "eur", "EUR" },
                { "euro", "EUR" },
                { "gbp", "GBP" },
                { "sterling", "GBP" },
                { "pound sterling", "GBP" }
            };

        internal static EntityReference ResolveTransactionCurrency(IOrganizationService service, string inputValue)
        {
            string normalizedValue = NormalizeCurrencyCode(inputValue);

            return ResolveSingleLookup(
                service,
                "transactioncurrency",
                "isocurrencycode",
                normalizedValue,
                "transaction currency");
        }

        internal static string NormalizeCurrencyCode(string inputValue)
        {
            string trimmedValue = inputValue.Trim();
            if (CurrencyAliases.TryGetValue(trimmedValue, out string aliasValue))
            {
                return aliasValue;
            }

            return trimmedValue.ToUpperInvariant();
        }

        internal static EntityReference ResolveSingleLookup(
            IOrganizationService service,
            string entityLogicalName,
            string filterAttributeName,
            string filterValue,
            string recordLabel)
        {
            QueryExpression query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 2
            };

            query.Criteria.AddCondition(filterAttributeName, ConditionOperator.Equal, filterValue);

            EntityCollection records = service.RetrieveMultiple(query);
            if (records.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No {0} record was found for '{1}'.",
                        recordLabel,
                        filterValue));
            }

            if (records.Entities.Count > 1)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Multiple {0} records were found for '{1}'. Please provide a unique value.",
                        recordLabel,
                        filterValue));
            }

            Entity record = records.Entities[0];
            return new EntityReference(entityLogicalName, record.Id);
        }

        internal static EntityReference GetCreatedEntityReference(
            EntityReferenceCollection createdEntities,
            string entityLogicalName)
        {
            foreach (EntityReference createdEntity in createdEntities)
            {
                if (string.Equals(createdEntity.LogicalName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
                {
                    return createdEntity;
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The qualify lead request did not return a created '{0}' record.",
                    entityLogicalName));
        }

        internal static void AddAttributeIfPresent(
            Entity entity,
            string attributeName,
            object value,
            OpportunityAttributeValueResolver attributeValueResolver)
        {
            if (value == null)
            {
                return;
            }

            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                return;
            }

            attributeValueResolver.SetAttribute(entity, attributeName, value);
        }

        internal static EntityReference GetRequiredEntityReferenceAttribute(
            Entity entity,
            string attributeName)
        {
            EntityReference attributeValue = entity.GetAttributeValue<EntityReference>(attributeName);
            if (attributeValue != null)
            {
                return attributeValue;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The '{0}' record '{1}' does not contain a value for required attribute '{2}'.",
                    entity.LogicalName,
                    entity.Id,
                    attributeName));
        }

        internal static void MoveOpportunityBusinessProcessFlowToStage(
            IOrganizationService service,
            ITracingService tracingService,
            EntityReference opportunityReference,
            string investmentProgrammeCode)
        {
            Guid[] requiredStageSequence = new Guid[0];
            if (investmentProgrammeCode.Contains("BPC"))
            {
                requiredStageSequence = GetRequiredBPCDirectOpportunityBpfStageSequence(investmentProgrammeCode);
            }
            RetrieveProcessInstancesRequest retrieveProcessInstancesRequest = new RetrieveProcessInstancesRequest
            {
                EntityId = opportunityReference.Id,
                EntityLogicalName = opportunityReference.LogicalName
            };
            RetrieveProcessInstancesResponse retrieveProcessInstancesResponse =
                (RetrieveProcessInstancesResponse)service.Execute(retrieveProcessInstancesRequest);

            if (retrieveProcessInstancesResponse.Processes.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No business process flow instance was found for opportunity '{0}'.",
                        opportunityReference.Id));
            }

            Entity activeProcessInstance = retrieveProcessInstancesResponse.Processes.Entities[0];
            if (string.IsNullOrWhiteSpace(activeProcessInstance.LogicalName))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The business process flow instance for opportunity '{0}' did not include a logical name.",
                        opportunityReference.Id));
            }

            string processInstanceLogicalName = ResolveProcessInstanceLogicalName(service, activeProcessInstance);
            Guid currentActiveStageId = GetRequiredGuidAttribute(
                activeProcessInstance,
                CreateLeadOpportunityKycRequestConstant.ProcessStageIdAttributeName,
                "business process flow instance");
            int currentStageSequencePosition = GetStageSequencePosition(requiredStageSequence, currentActiveStageId);
            if (currentStageSequencePosition < 0)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The active business process flow stage '{0}' for opportunity '{1}' is not part of the expected sequence for programme code '{2}'.",
                        currentActiveStageId,
                        opportunityReference.Id,
                        investmentProgrammeCode));
            }

            RetrieveActivePathRequest retrieveActivePathRequest = new RetrieveActivePathRequest
            {
                ProcessInstanceId = activeProcessInstance.Id
            };
            RetrieveActivePathResponse retrieveActivePathResponse =
                (RetrieveActivePathResponse)service.Execute(retrieveActivePathRequest);

            ValidateRequiredStageSequence(
                retrieveActivePathResponse.ProcessStages,
                requiredStageSequence,
                opportunityReference.Id,
                investmentProgrammeCode);

            tracingService.Trace(
                "Active BPF instance '{0}' ({1}) is at stage '{2}' for opportunity '{3}'.",
                activeProcessInstance.Id,
                processInstanceLogicalName,
                currentActiveStageId,
                opportunityReference.Id);

            if (currentStageSequencePosition == requiredStageSequence.Length - 1)
            {
                string traversedPath = BuildTraversedPath(requiredStageSequence, requiredStageSequence.Length);
                UpdateProcessInstanceStage(
                    service,
                    processInstanceLogicalName,
                    activeProcessInstance.Id,
                    requiredStageSequence[currentStageSequencePosition],
                    traversedPath);
                tracingService.Trace(
                    "BPF instance '{0}' already at target stage '{1}'. Traversed path normalized to '{2}'.",
                    activeProcessInstance.Id,
                    requiredStageSequence[currentStageSequencePosition],
                    traversedPath);
                return;
            }

            for (int stageSequencePosition = currentStageSequencePosition + 1;
                stageSequencePosition < requiredStageSequence.Length;
                stageSequencePosition++)
            {
                Guid targetStageId = requiredStageSequence[stageSequencePosition];
                string traversedPath = BuildTraversedPath(requiredStageSequence, stageSequencePosition + 1);
                UpdateProcessInstanceStage(
                    service,
                    processInstanceLogicalName,
                    activeProcessInstance.Id,
                    targetStageId,
                    traversedPath);
                tracingService.Trace(
                    "Moved BPF instance '{0}' to stage '{1}' with traversed path '{2}'.",
                    activeProcessInstance.Id,
                    targetStageId,
                    traversedPath);
            }
        }

        private static Guid[] GetRequiredBPCDirectOpportunityBpfStageSequence(string investmentProgrammeCode)
        {
            if (string.Equals(
                investmentProgrammeCode,
                CreateLeadOpportunityKycRequestConstant.InvestmentProgrammeCodeBPCCoreFund,
                StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    CreateLeadOpportunityKycRequestConstant.BPCDirectLeadBpfMarketEngagementStageId,
                    CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfEoiStageId,
                    CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfFormalProposalBpcCoreStageId,
                    CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfDdStageId
                };
            }

            return new[]
            {
                CreateLeadOpportunityKycRequestConstant.BPCDirectLeadBpfMarketEngagementStageId,
                CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfEoiStageId,
                CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfFormalProposalNonBpcCoreStageId,
                CreateLeadOpportunityKycRequestConstant.BPCDirectOpportunityBpfDdStageId
            };
        }

        private static void ValidateRequiredStageSequence(
            EntityCollection processStages,
            Guid[] requiredStageSequence,
            Guid opportunityId,
            string investmentProgrammeCode)
        {
            if (processStages.Entities.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No active business process flow path was returned for opportunity '{0}'.",
                        opportunityId));
            }

            int previousPathPosition = -1;
            for (int sequencePosition = 0; sequencePosition < requiredStageSequence.Length; sequencePosition++)
            {
                int currentPathPosition = FindProcessStagePosition(processStages, requiredStageSequence[sequencePosition]);
                if (currentPathPosition < 0)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Required stage '{0}' was not found in the active business process flow path for opportunity '{1}' and programme code '{2}'.",
                            requiredStageSequence[sequencePosition],
                            opportunityId,
                            investmentProgrammeCode));
                }

                if (previousPathPosition >= 0 && currentPathPosition != previousPathPosition + 1)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The configured business process flow stages for programme code '{0}' are not consecutive in the active path for opportunity '{1}'.",
                            investmentProgrammeCode,
                            opportunityId));
                }

                previousPathPosition = currentPathPosition;
            }
        }

        private static int FindProcessStagePosition(EntityCollection processStages, Guid stageId)
        {
            for (int processStagePosition = 0; processStagePosition < processStages.Entities.Count; processStagePosition++)
            {
                Guid currentStageId = GetRequiredGuidAttribute(
                    processStages.Entities[processStagePosition],
                    CreateLeadOpportunityKycRequestConstant.ProcessStageIdAttributeName,
                    "process stage");
                if (currentStageId == stageId)
                {
                    return processStagePosition;
                }
            }

            return -1;
        }

        private static int GetStageSequencePosition(Guid[] stageSequence, Guid stageId)
        {
            for (int stageSequencePosition = 0; stageSequencePosition < stageSequence.Length; stageSequencePosition++)
            {
                if (stageSequence[stageSequencePosition] == stageId)
                {
                    return stageSequencePosition;
                }
            }

            return -1;
        }

        private static string ResolveProcessInstanceLogicalName(
            IOrganizationService service,
            Entity activeProcessInstance)
        {
            Guid processDefinitionId = GetRequiredGuidAttribute(
                activeProcessInstance,
                CreateLeadOpportunityKycRequestConstant.ProcessIdAttributeName,
                "business process flow instance");
            Entity processDefinition = service.Retrieve(
                CreateLeadOpportunityKycRequestConstant.WorkflowEntityLogicalName,
                processDefinitionId,
                new ColumnSet(CreateLeadOpportunityKycRequestConstant.WorkflowUniqueNameAttributeName));
            string processInstanceLogicalName = processDefinition.GetAttributeValue<string>(
                CreateLeadOpportunityKycRequestConstant.WorkflowUniqueNameAttributeName);
            if (!string.IsNullOrWhiteSpace(processInstanceLogicalName))
            {
                return processInstanceLogicalName;
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The business process flow definition '{0}' does not contain a '{1}' value.",
                    processDefinitionId,
                    CreateLeadOpportunityKycRequestConstant.WorkflowUniqueNameAttributeName));
        }

        private static Guid GetRequiredGuidAttribute(Entity entity, string attributeName, string recordLabel)
        {
            if (!entity.Attributes.Contains(attributeName))
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The {0} record '{1}' does not contain required attribute '{2}'.",
                        recordLabel,
                        entity.Id,
                        attributeName));
            }

            object attributeValue = entity[attributeName];
            if (attributeValue is Guid guidValue && guidValue != Guid.Empty)
            {
                return guidValue;
            }

            if (attributeValue is EntityReference entityReference && entityReference.Id != Guid.Empty)
            {
                return entityReference.Id;
            }

            if (attributeValue != null)
            {
                Guid parsedGuid;
                if (Guid.TryParse(attributeValue.ToString(), out parsedGuid) && parsedGuid != Guid.Empty)
                {
                    return parsedGuid;
                }
            }

            throw new InvalidPluginExecutionException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The {0} record '{1}' contains an invalid value for attribute '{2}'.",
                    recordLabel,
                    entity.Id,
                    attributeName));
        }

        private static string BuildTraversedPath(Guid[] stageSequence, int stageCount)
        {
            string[] traversedStageIds = new string[stageCount];
            for (int stageSequencePosition = 0; stageSequencePosition < stageCount; stageSequencePosition++)
            {
                traversedStageIds[stageSequencePosition] = stageSequence[stageSequencePosition].ToString();
            }

            return string.Join(",", traversedStageIds);
        }

        private static void UpdateProcessInstanceStage(
            IOrganizationService service,
            string processInstanceLogicalName,
            Guid processInstanceId,
            Guid activeStageId,
            string traversedPath)
        {
            Entity processInstance = new Entity(processInstanceLogicalName, processInstanceId);
            processInstance[CreateLeadOpportunityKycRequestConstant.ActiveStageIdAttributeName] =
                new EntityReference(
                    CreateLeadOpportunityKycRequestConstant.ProcessStageEntityLogicalName,
                    activeStageId);
            processInstance[CreateLeadOpportunityKycRequestConstant.TraversedPathAttributeName] = traversedPath;
            service.Update(processInstance);
        }
    }
}
