using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace custom_api_plugin
{
    public class BPCDirectAffinity : PluginBase
    {
        private const int QualifiedLeadStatusCode = 3;
        private const string LeadParameterName = "ukn_lead";
        private const string OpportunityParameterName = "ukn_opportunity";
        private const string LeadIdOutputParameterName = "ukn_leadid";
        private const string OpportunityIdOutputParameterName = "ukn_opportunityid";

        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        private static readonly ISet<string> LeadIntegrationAttributeNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ukn_subject",
                "ukn_transactioncurrencyid",
                "ukn_parentaccount",
                "ukn_accountlead",
                "ukn_dealname",
                "ukn_initialcontactdate",
                "ukn_parentcontact",
                "ukn_sector",
                "ukn_investmentprogramme",
                "ukn_expectedbbbcommitment",
                "leadid",
                "@odata.type"
            };

        private static readonly ISet<string> OpportunityIgnoredAttributeNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "opportunityid",
                "@odata.type"
            };

        public BPCDirectAffinity(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(BPCDirectAffinity))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            ExecuteDirectAffinity(localPluginContext);
        }

        private static void ExecuteDirectAffinity(ILocalPluginContext localPluginContext)
        {
            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;
            IOrganizationService service = localPluginContext.InitiatingUserService;
            ITracingService tracingService = localPluginContext.TracingService;

            tracingService.Trace("Reading and validating JSON Custom API request parameters.");
            BpcDirectAffinityRequest request = BpcDirectAffinityRequest.FromContext(context);
            AttributeValueResolver attributeValueResolver = new AttributeValueResolver(service);

            tracingService.Trace("Creating lead from JSON string Custom API parameters.");
            tracingService.Trace(
                "Validated JSON payloads with {0} lead attribute(s) and {1} opportunity attribute(s).",
                request.LeadPayload.Count,
                request.OpportunityPayload.Count);

            tracingService.Trace("Resolving transaction currency from input '{0}'.", request.CurrencyInput);
            EntityReference transactionCurrency = DataverseEntityHelper.ResolveTransactionCurrency(service, request.CurrencyInput);
            tracingService.Trace("Resolved transaction currency to record Id '{0}'.", transactionCurrency.Id);

            tracingService.Trace("Resolving parent account from account name '{0}'.", request.ParentAccountName);
            EntityReference parentAccount = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "account",
                "name",
                request.ParentAccountName,
                "account");
            tracingService.Trace("Resolved parent account to record Id '{0}'.", parentAccount.Id);

            tracingService.Trace("Resolving account lead from internal email address '{0}'.", request.AccountLeadEmailAddress);
            EntityReference accountLead = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "systemuser",
                "internalemailaddress",
                request.AccountLeadEmailAddress,
                "account lead");
            tracingService.Trace("Resolved account lead to record Id '{0}'.", accountLead.Id);

            tracingService.Trace("Resolving parent contact from email address '{0}'.", request.ParentContactEmailAddress);
            EntityReference parentContact = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "contact",
                "emailaddress1",
                request.ParentContactEmailAddress,
                "parent contact");
            tracingService.Trace("Resolved parent contact to record Id '{0}'.", parentContact.Id);

            tracingService.Trace("Resolving investment programme from code '{0}'.", request.InvestmentProgrammeCode);
            EntityReference investmentProgramme = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "bbb_investmentprogramme",
                "ukn_programmecodereference",
                request.InvestmentProgrammeCode,
                "investment programme");
            tracingService.Trace("Resolved investment programme to record Id '{0}'.", investmentProgramme.Id);

            tracingService.Trace("Building lead entity from JSON payload and resolved references.");
            Entity lead = BuildLeadEntity(
                request,
                transactionCurrency,
                parentAccount,
                accountLead,
                parentContact,
                investmentProgramme,
                attributeValueResolver);
            tracingService.Trace("Lead entity contains {0} attribute(s) before create.", lead.Attributes.Count);

            tracingService.Trace("Creating lead record in Dataverse.");
            Guid leadId = service.Create(lead);
            tracingService.Trace("Lead created with Id: {0}", leadId);

            tracingService.Trace("Qualifying lead '{0}' to create opportunity.", leadId);
            QualifyLeadRequest qualifyLeadRequest = new QualifyLeadRequest
            {
                LeadId = new EntityReference("lead", leadId),
                CreateAccount = false,
                CreateContact = false,
                CreateOpportunity = true,
                OpportunityCurrencyId = transactionCurrency,
                Status = new OptionSetValue(QualifiedLeadStatusCode)
            };

            tracingService.Trace("Executing QualifyLeadRequest for lead '{0}'.", leadId);
            QualifyLeadResponse qualifyLeadResponse = (QualifyLeadResponse)service.Execute(qualifyLeadRequest);
            EntityReference opportunityReference = DataverseEntityHelper.GetCreatedEntityReference(
                qualifyLeadResponse.CreatedEntities,
                "opportunity");

            tracingService.Trace("Opportunity created with Id: {0}", opportunityReference.Id);

            tracingService.Trace("Building opportunity update entity from JSON payload.");
            Entity opportunity = BuildOpportunityUpdateEntity(
                request.OpportunityPayload,
                opportunityReference,
                attributeValueResolver);
            tracingService.Trace(
                "Opportunity update entity contains {0} attribute(s) after filtering ignored fields.",
                opportunity.Attributes.Count);
            if (opportunity.Attributes.Count > 0)
            {
                tracingService.Trace("Updating opportunity '{0}' with payload attributes.", opportunityReference.Id);
                service.Update(opportunity);
                tracingService.Trace(
                    "Opportunity '{0}' updated with {1} provided attribute(s).",
                    opportunityReference.Id,
                    opportunity.Attributes.Count);
            }
            else
            {
                tracingService.Trace(
                    "No opportunity attributes remained after filtering; skipping update for opportunity '{0}'.",
                    opportunityReference.Id);
            }

            context.OutputParameters[LeadIdOutputParameterName] = leadId;
            context.OutputParameters[OpportunityIdOutputParameterName] = opportunityReference.Id;
            tracingService.Trace(
                "Custom API output parameters populated with lead Id '{0}' and opportunity Id '{1}'.",
                leadId,
                opportunityReference.Id);
        }

        private static Entity BuildLeadEntity(
            BpcDirectAffinityRequest request,
            EntityReference transactionCurrency,
            EntityReference parentAccount,
            EntityReference accountLead,
            EntityReference parentContact,
            EntityReference investmentProgramme,
            AttributeValueResolver attributeValueResolver)
        {
            Entity lead = new Entity("lead");
            CopyAttributes(
                request.LeadPayload,
                lead,
                LeadIntegrationAttributeNames,
                attributeValueResolver);

            lead["subject"] = request.Subject;

            if (!lead.Attributes.Contains("fullname"))
            {
                lead["fullname"] = request.Subject;
            }

            lead["bbb_dealname"] = request.DealName;
            lead["companyname"] = request.ParentAccountName;
            lead["transactioncurrencyid"] = transactionCurrency;
            lead["parentaccountid"] = parentAccount;
            lead["parentcontactid"] = parentContact;
            lead["bbb_investmentprogramme"] = investmentProgramme;
            lead["ukn_sector"] = new OptionSetValue(request.SectorValue);
            lead["ukn_initialcontactdate"] = request.InitialContactDate;
            lead["ukn_accountlead"] = accountLead;
            lead["ukn_expectedbbbcommitment"] = request.ExpectedCommitment;

            return lead;
        }

        private static Entity BuildOpportunityUpdateEntity(
            IDictionary<string, object> opportunityPayload,
            EntityReference opportunityReference,
            AttributeValueResolver attributeValueResolver)
        {
            Entity opportunity = new Entity(opportunityReference.LogicalName, opportunityReference.Id);
            CopyAttributes(
                opportunityPayload,
                opportunity,
                OpportunityIgnoredAttributeNames,
                attributeValueResolver);
            return opportunity;
        }

        private static void CopyAttributes(
            IDictionary<string, object> source,
            Entity target,
            ISet<string> excludedAttributeNames,
            AttributeValueResolver attributeValueResolver)
        {
            foreach (KeyValuePair<string, object> attribute in source)
            {
                if (excludedAttributeNames.Contains(attribute.Key))
                {
                    continue;
                }

                target[attribute.Key] = attributeValueResolver.Resolve(
                    target.LogicalName,
                    attribute.Key,
                    attribute.Value);
            }
        }

        private sealed class BpcDirectAffinityRequest
        {
            public IDictionary<string, object> LeadPayload { get; private set; }

            public IDictionary<string, object> OpportunityPayload { get; private set; }

            public string Subject { get; private set; }

            public string CurrencyInput { get; private set; }

            public string ParentAccountName { get; private set; }

            public string AccountLeadEmailAddress { get; private set; }

            public string DealName { get; private set; }

            public DateTime InitialContactDate { get; private set; }

            public string ParentContactEmailAddress { get; private set; }

            public int SectorValue { get; private set; }

            public string InvestmentProgrammeCode { get; private set; }

            public Money ExpectedCommitment { get; private set; }

            public static BpcDirectAffinityRequest FromContext(IPluginExecutionContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                IDictionary<string, object> leadPayload = GetRequiredJsonParameter(context, LeadParameterName);
                IDictionary<string, object> opportunityPayload = GetRequiredJsonParameter(context, OpportunityParameterName);

                return new BpcDirectAffinityRequest
                {
                    LeadPayload = leadPayload,
                    OpportunityPayload = opportunityPayload,
                    Subject = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_subject"),
                    CurrencyInput = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_transactioncurrencyid"),
                    ParentAccountName = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_parentaccount"),
                    AccountLeadEmailAddress = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_accountlead"),
                    DealName = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_dealname"),
                    InitialContactDate = GetRequiredDateAttribute(leadPayload, LeadParameterName, "ukn_initialcontactdate"),
                    ParentContactEmailAddress = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_parentcontact"),
                    SectorValue = GetRequiredIntegerAttribute(leadPayload, LeadParameterName, "ukn_sector"),
                    InvestmentProgrammeCode = GetRequiredStringAttribute(leadPayload, LeadParameterName, "ukn_investmentprogramme"),
                    ExpectedCommitment = GetRequiredMoneyAttribute(leadPayload, LeadParameterName, "ukn_expectedbbbcommitment")
                };
            }

            private static IDictionary<string, object> GetRequiredJsonParameter(
                IPluginExecutionContext context,
                string parameterName)
            {
                if (!context.InputParameters.Contains(parameterName) || context.InputParameters[parameterName] == null)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The custom API parameter '{0}' is required.",
                            parameterName));
                }

                if (!(context.InputParameters[parameterName] is string jsonValue)
                    || string.IsNullOrWhiteSpace(jsonValue))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The custom API parameter '{0}' must contain a JSON string payload.",
                            parameterName));
                }

                object deserializedPayload;
                try
                {
                    deserializedPayload = JsonSerializer.DeserializeObject(jsonValue);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The custom API parameter '{0}' must contain valid JSON.",
                            parameterName),
                        ex);
                }

                if (!(deserializedPayload is IDictionary<string, object> payload))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The custom API parameter '{0}' must contain a JSON object.",
                            parameterName));
                }

                return payload;
            }

            private static string GetRequiredStringAttribute(
                IDictionary<string, object> payload,
                string parameterName,
                string attributeName)
            {
                object value = GetRequiredAttributeValue(payload, parameterName, attributeName);
                if (!(value is string stringValue) || string.IsNullOrWhiteSpace(stringValue))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The '{0}' entity payload must contain a non-empty '{1}' value.",
                            parameterName,
                            attributeName));
                }

                return stringValue.Trim();
            }

            private static Money GetRequiredMoneyAttribute(
                IDictionary<string, object> payload,
                string parameterName,
                string attributeName)
            {
                object value = GetRequiredAttributeValue(payload, parameterName, attributeName);

                if (value is Money money)
                {
                    return money;
                }

                if (value is decimal decimalValue)
                {
                    return new Money(decimalValue);
                }

                if (value is double doubleValue)
                {
                    return new Money(Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture));
                }

                if (value is int intValue)
                {
                    return new Money(intValue);
                }

                if (value is long longValue)
                {
                    return new Money(longValue);
                }

                if (value is string stringValue
                    && decimal.TryParse(
                        stringValue,
                        NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                        CultureInfo.InvariantCulture,
                        out decimal parsedValue))
                {
                    return new Money(parsedValue);
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The '{0}' entity payload must contain a numeric '{1}' value.",
                        parameterName,
                        attributeName));
            }

            private static DateTime GetRequiredDateAttribute(
                IDictionary<string, object> payload,
                string parameterName,
                string attributeName)
            {
                object value = GetRequiredAttributeValue(payload, parameterName, attributeName);
                if (CustomApiDateParser.TryParseDate(value, out DateTime parsedDate))
                {
                    return parsedDate;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The '{0}' entity payload must contain a valid date '{1}' value.",
                        parameterName,
                        attributeName));
            }

            private static int GetRequiredIntegerAttribute(
                IDictionary<string, object> payload,
                string parameterName,
                string attributeName)
            {
                object value = GetRequiredAttributeValue(payload, parameterName, attributeName);
                if (value is int intValue)
                {
                    return intValue;
                }

                if (value is OptionSetValue optionSetValue)
                {
                    return optionSetValue.Value;
                }

                if (value is long longValue
                    && longValue >= int.MinValue
                    && longValue <= int.MaxValue)
                {
                    return Convert.ToInt32(longValue, CultureInfo.InvariantCulture);
                }

                if (value is string stringValue
                    && int.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsedValue))
                {
                    return parsedValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The '{0}' entity payload must contain an integer '{1}' value.",
                        parameterName,
                        attributeName));
            }

            private static object GetRequiredAttributeValue(
                IDictionary<string, object> payload,
                string parameterName,
                string attributeName)
            {
                if (!payload.ContainsKey(attributeName) || payload[attributeName] == null)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The '{0}' entity payload must contain the '{1}' attribute.",
                            parameterName,
                            attributeName));
                }

                return payload[attributeName];
            }
        }

        private sealed class AttributeValueResolver
        {
            private readonly IOrganizationService service;
            private readonly IDictionary<string, AttributeMetadata> attributeMetadataCache;
            private readonly IDictionary<string, string> primaryNameAttributeCache;

            public AttributeValueResolver(IOrganizationService service)
            {
                this.service = service ?? throw new ArgumentNullException(nameof(service));
                attributeMetadataCache = new Dictionary<string, AttributeMetadata>(StringComparer.OrdinalIgnoreCase);
                primaryNameAttributeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public object Resolve(string entityLogicalName, string attributeName, object rawValue)
            {
                if (rawValue == null)
                {
                    return null;
                }

                AttributeMetadata attributeMetadata = GetAttributeMetadata(entityLogicalName, attributeName);
                if (!attributeMetadata.AttributeType.HasValue)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The attribute '{0}' on entity '{1}' does not expose a supported metadata type.",
                            attributeName,
                            entityLogicalName));
                }

                switch (attributeMetadata.AttributeType.Value)
                {
                    case AttributeTypeCode.String:
                    case AttributeTypeCode.Memo:
                    case AttributeTypeCode.EntityName:
                        return GetStringValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Boolean:
                        return GetBooleanValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Integer:
                        return GetIntegerValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.BigInt:
                        return GetLongValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Decimal:
                        return GetDecimalValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Double:
                        return GetDoubleValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Money:
                        return new Money(GetDecimalValue(rawValue, entityLogicalName, attributeName));

                    case AttributeTypeCode.DateTime:
                        return GetDateValue(rawValue, entityLogicalName, attributeName);

                    case AttributeTypeCode.Picklist:
                    case AttributeTypeCode.State:
                    case AttributeTypeCode.Status:
                        return new OptionSetValue(GetIntegerValue(rawValue, entityLogicalName, attributeName));

                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Owner:
                        return ResolveLookupValue(
                            entityLogicalName,
                            attributeName,
                            rawValue,
                            (LookupAttributeMetadata)attributeMetadata);

                    case AttributeTypeCode.Uniqueidentifier:
                        return GetGuidValue(rawValue, entityLogicalName, attributeName);

                    default:
                        throw new InvalidPluginExecutionException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "The attribute '{0}' on entity '{1}' is not supported by the Affinity JSON payload contract.",
                                attributeName,
                                entityLogicalName));
                }
            }

            private AttributeMetadata GetAttributeMetadata(string entityLogicalName, string attributeName)
            {
                string cacheKey = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}",
                    entityLogicalName,
                    attributeName);

                if (attributeMetadataCache.TryGetValue(cacheKey, out AttributeMetadata attributeMetadata))
                {
                    return attributeMetadata;
                }

                RetrieveAttributeRequest request = new RetrieveAttributeRequest
                {
                    EntityLogicalName = entityLogicalName,
                    LogicalName = attributeName,
                    RetrieveAsIfPublished = true
                };

                RetrieveAttributeResponse response = (RetrieveAttributeResponse)service.Execute(request);
                attributeMetadata = response.AttributeMetadata;
                attributeMetadataCache[cacheKey] = attributeMetadata;
                return attributeMetadata;
            }

            private EntityReference ResolveLookupValue(
                string entityLogicalName,
                string attributeName,
                object rawValue,
                LookupAttributeMetadata lookupMetadata)
            {
                if (rawValue is string stringValue)
                {
                    string trimmedValue = stringValue.Trim();
                    string targetEntityLogicalName = GetSingleLookupTarget(lookupMetadata, entityLogicalName, attributeName);

                    if (Guid.TryParse(trimmedValue, out Guid parsedGuid))
                    {
                        return new EntityReference(targetEntityLogicalName, parsedGuid);
                    }

                    string primaryNameAttribute = GetPrimaryNameAttribute(targetEntityLogicalName);
                    return DataverseEntityHelper.ResolveSingleLookup(
                        service,
                        targetEntityLogicalName,
                        primaryNameAttribute,
                        trimmedValue,
                        attributeName);
                }

                if (!(rawValue is IDictionary<string, object> lookupObject))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The lookup attribute '{0}' on entity '{1}' must be provided as a string, GUID string, or lookup descriptor object.",
                            attributeName,
                            entityLogicalName));
                }

                if (lookupObject.ContainsKey("id") && lookupObject["id"] != null)
                {
                    string targetEntityLogicalName = GetTargetEntityLogicalName(
                        lookupMetadata,
                        entityLogicalName,
                        attributeName,
                        lookupObject);
                    Guid lookupId = GetGuidValue(lookupObject["id"], entityLogicalName, attributeName);
                    return new EntityReference(targetEntityLogicalName, lookupId);
                }

                string filterValue = GetLookupObjectStringValue(
                    lookupObject,
                    "value",
                    entityLogicalName,
                    attributeName);
                string resolvedTargetEntityLogicalName = GetTargetEntityLogicalName(
                    lookupMetadata,
                    entityLogicalName,
                    attributeName,
                    lookupObject);
                string filterAttributeName = GetOptionalLookupObjectStringValue(
                    lookupObject,
                    "filterAttributeName");

                if (string.IsNullOrWhiteSpace(filterAttributeName))
                {
                    filterAttributeName = GetPrimaryNameAttribute(resolvedTargetEntityLogicalName);
                }

                return DataverseEntityHelper.ResolveSingleLookup(
                    service,
                    resolvedTargetEntityLogicalName,
                    filterAttributeName,
                    filterValue,
                    attributeName);
            }

            private string GetTargetEntityLogicalName(
                LookupAttributeMetadata lookupMetadata,
                string entityLogicalName,
                string attributeName,
                IDictionary<string, object> lookupObject)
            {
                string explicitTarget = GetOptionalLookupObjectStringValue(
                    lookupObject,
                    "targetEntityLogicalName");

                if (!string.IsNullOrWhiteSpace(explicitTarget))
                {
                    return explicitTarget.Trim();
                }

                return GetSingleLookupTarget(lookupMetadata, entityLogicalName, attributeName);
            }

            private string GetSingleLookupTarget(
                LookupAttributeMetadata lookupMetadata,
                string entityLogicalName,
                string attributeName)
            {
                if (lookupMetadata.Targets == null || lookupMetadata.Targets.Length != 1)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The lookup attribute '{0}' on entity '{1}' targets multiple tables. Provide 'targetEntityLogicalName' in the JSON payload.",
                            attributeName,
                            entityLogicalName));
                }

                return lookupMetadata.Targets[0];
            }

            private string GetPrimaryNameAttribute(string entityLogicalName)
            {
                if (primaryNameAttributeCache.TryGetValue(entityLogicalName, out string primaryNameAttribute))
                {
                    return primaryNameAttribute;
                }

                RetrieveEntityRequest request = new RetrieveEntityRequest
                {
                    LogicalName = entityLogicalName,
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                };

                RetrieveEntityResponse response = (RetrieveEntityResponse)service.Execute(request);
                primaryNameAttribute = response.EntityMetadata.PrimaryNameAttribute;
                if (string.IsNullOrWhiteSpace(primaryNameAttribute))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The target entity '{0}' does not expose a primary name attribute for lookup resolution.",
                            entityLogicalName));
                }

                primaryNameAttributeCache[entityLogicalName] = primaryNameAttribute;
                return primaryNameAttribute;
            }

            private static string GetLookupObjectStringValue(
                IDictionary<string, object> lookupObject,
                string propertyName,
                string entityLogicalName,
                string attributeName)
            {
                if (!lookupObject.ContainsKey(propertyName) || lookupObject[propertyName] == null)
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The lookup descriptor for attribute '{0}' on entity '{1}' must include a '{2}' property.",
                            attributeName,
                            entityLogicalName,
                            propertyName));
                }

                if (!(lookupObject[propertyName] is string stringValue) || string.IsNullOrWhiteSpace(stringValue))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The lookup descriptor property '{0}' for attribute '{1}' on entity '{2}' must be a non-empty string.",
                            propertyName,
                            attributeName,
                            entityLogicalName));
                }

                return stringValue.Trim();
            }

            private static string GetOptionalLookupObjectStringValue(
                IDictionary<string, object> lookupObject,
                string propertyName)
            {
                if (!lookupObject.ContainsKey(propertyName) || lookupObject[propertyName] == null)
                {
                    return null;
                }

                return lookupObject[propertyName] as string;
            }

            private static string GetStringValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (!(rawValue is string stringValue))
                {
                    throw new InvalidPluginExecutionException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The attribute '{0}' on entity '{1}' must be provided as a string.",
                            attributeName,
                            entityLogicalName));
                }

                return stringValue;
            }

            private static bool GetBooleanValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is bool boolValue)
                {
                    return boolValue;
                }

                if (rawValue is string stringValue
                    && bool.TryParse(stringValue.Trim(), out bool parsedValue))
                {
                    return parsedValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a boolean value.",
                        attributeName,
                        entityLogicalName));
            }

            private static int GetIntegerValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is int intValue)
                {
                    return intValue;
                }

                if (rawValue is long longValue
                    && longValue >= int.MinValue
                    && longValue <= int.MaxValue)
                {
                    return Convert.ToInt32(longValue, CultureInfo.InvariantCulture);
                }

                if (rawValue is decimal decimalValue
                    && decimal.Truncate(decimalValue) == decimalValue
                    && decimalValue >= int.MinValue
                    && decimalValue <= int.MaxValue)
                {
                    return decimal.ToInt32(decimalValue);
                }

                if (rawValue is double doubleValue
                    && Math.Abs(doubleValue % 1) < double.Epsilon
                    && doubleValue >= int.MinValue
                    && doubleValue <= int.MaxValue)
                {
                    return Convert.ToInt32(doubleValue, CultureInfo.InvariantCulture);
                }

                if (rawValue is string stringValue
                    && int.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsedIntegerValue))
                {
                    return parsedIntegerValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be an integer value.",
                        attributeName,
                        entityLogicalName));
            }

            private static long GetLongValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is long longValue)
                {
                    return longValue;
                }

                if (rawValue is int intValue)
                {
                    return intValue;
                }

                if (rawValue is decimal decimalValue
                    && decimal.Truncate(decimalValue) == decimalValue
                    && decimalValue >= long.MinValue
                    && decimalValue <= long.MaxValue)
                {
                    return decimal.ToInt64(decimalValue);
                }

                if (rawValue is string stringValue
                    && long.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out long parsedLongValue))
                {
                    return parsedLongValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a whole number value.",
                        attributeName,
                        entityLogicalName));
            }

            private static decimal GetDecimalValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is decimal decimalValue)
                {
                    return decimalValue;
                }

                if (rawValue is int intValue)
                {
                    return intValue;
                }

                if (rawValue is long longValue)
                {
                    return longValue;
                }

                if (rawValue is double doubleValue)
                {
                    return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                }

                if (rawValue is string stringValue
                    && decimal.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                        CultureInfo.InvariantCulture,
                        out decimal parsedDecimalValue))
                {
                    return parsedDecimalValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a numeric value.",
                        attributeName,
                        entityLogicalName));
            }

            private static double GetDoubleValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is double doubleValue)
                {
                    return doubleValue;
                }

                if (rawValue is decimal decimalValue)
                {
                    return Convert.ToDouble(decimalValue, CultureInfo.InvariantCulture);
                }

                if (rawValue is int intValue)
                {
                    return intValue;
                }

                if (rawValue is long longValue)
                {
                    return longValue;
                }

                if (rawValue is string stringValue
                    && double.TryParse(
                        stringValue.Trim(),
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out double parsedDoubleValue))
                {
                    return parsedDoubleValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a floating-point numeric value.",
                        attributeName,
                        entityLogicalName));
            }

            private static DateTime GetDateValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (CustomApiDateParser.TryParseDateTime(rawValue, out DateTime parsedDateValue))
                {
                    return parsedDateValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a valid date value.",
                        attributeName,
                        entityLogicalName));
            }

            private static Guid GetGuidValue(object rawValue, string entityLogicalName, string attributeName)
            {
                if (rawValue is Guid guidValue)
                {
                    return guidValue;
                }

                if (rawValue is string stringValue
                    && Guid.TryParse(stringValue.Trim(), out Guid parsedGuidValue))
                {
                    return parsedGuidValue;
                }

                throw new InvalidPluginExecutionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The attribute '{0}' on entity '{1}' must be a GUID value.",
                        attributeName,
                        entityLogicalName));
            }
        }
    }
}