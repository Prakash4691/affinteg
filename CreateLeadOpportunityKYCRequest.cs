using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Globalization;
using CreateLeadOpportunityKycRequestConstant = custom_api_plugin.Constant.CreateLeadOpportunityKYCRequestValues;

namespace custom_api_plugin
{
    public class CreateLeadOpportunityKYCRequest : PluginBase
    {
        public CreateLeadOpportunityKYCRequest(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(CreateLeadOpportunityKYCRequest))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new InvalidPluginExecutionException("The local plug-in context is required.");
            }

            ExecuteCreateLeadAndQualifyToOpportunity(localPluginContext);
        }

        private static void ExecuteCreateLeadAndQualifyToOpportunity(ILocalPluginContext localPluginContext)
        {
            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.InitiatingUserService;
            var tracingService = localPluginContext.TracingService;

            tracingService.Trace("Reading and validating Custom API request parameters for lead creation.");
            CreateLeadRequest request = CreateLeadRequest.FromContext(context);//BU specfic fields and few are generic fields
            UpdateOpportunityRequest updateOpportunityRequest = UpdateOpportunityRequest.FromContext(context);// Only BU specific fields

            tracingService.Trace("Creating lead from Custom API input parameters.");
            tracingService.Trace(
                "Validated request payload for subject '{0}', deal '{1}', programme '{2}', and sector '{3}'.",
                request.Subject,
                request.DealName,
                request.InvestmentProgrammeCode,
                request.SectorValue);

            tracingService.Trace("Resolving transaction currency from input '{0}'.", request.CurrencyInput);
            EntityReference transactionCurrency = DataverseEntityHelper.ResolveTransactionCurrency(service, request.CurrencyInput);
            tracingService.Trace("Resolved transaction currency to record Id '{0}'.", transactionCurrency.Id);

            tracingService.Trace("Resolving parent account from account Id '{0}'.", request.ParentAccountId);
            EntityReference parentAccount = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "account",
                "accountid",
                request.ParentAccountId,
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

            tracingService.Trace("Resolving parent contact from contact Id '{0}'.", request.ParentContactId);
            EntityReference parentContact = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "contact",
                "contactid",
                request.ParentContactId,
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

            EntityReference investmentSubProgramme = null;
            if (request.InvestmentSubProgrammeCode != null)
            {
                investmentSubProgramme = DataverseEntityHelper.ResolveSingleLookup(
                   service,
                   "bbb_investmentsubprogramme",
                   "ukn_investmentsubprogrammecategorycode",
                   request.InvestmentSubProgrammeCode,
                   "investment sub-programme");
                tracingService.Trace("Resolved investment sub-programme to record Id '{0}'.", investmentSubProgramme.Id);
            }

            tracingService.Trace("Building lead entity from resolved references and validated request data.");
            Entity lead = BuildLeadEntity(
                request,
                transactionCurrency,
                parentAccount,
                accountLead,
                parentContact,
                investmentProgramme,
                investmentSubProgramme);//BU specfic fields

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
                Status = new OptionSetValue(CreateLeadOpportunityKycRequestConstant.QualifiedLeadStatusCode)
            };

            tracingService.Trace("Executing QualifyLeadRequest for lead '{0}'.", leadId);
            QualifyLeadResponse qualifyLeadResponse = (QualifyLeadResponse)service.Execute(qualifyLeadRequest);
            tracingService.Trace(
                "Lead qualified with status code: {0}",
                CreateLeadOpportunityKycRequestConstant.QualifiedLeadStatusCode);

            EntityReference opportunityReference = DataverseEntityHelper.GetCreatedEntityReference(
                qualifyLeadResponse.CreatedEntities,
                CreateLeadOpportunityKycRequestConstant.OpportunityEntityLogicalName);
            tracingService.Trace("Opportunity created with Id: {0}", opportunityReference.Id);
            Entity opportunity = BuildOpportunityUpdateEntity(service, opportunityReference, updateOpportunityRequest);//BU specfic fields
            tracingService.Trace(
                "Opportunity update entity contains {0} attribute(s).",
                opportunity.Attributes.Count);
            service.Update(opportunity);
            tracingService.Trace(
                "Opportunity '{0}' updated with {1} attribute(s).",
                opportunityReference.Id,
                opportunity.Attributes.Count);
            tracingService.Trace(
                "Updating business process flow stage for opportunity '{0}' using programme code '{1}'.",
                opportunityReference.Id,
                request.InvestmentProgrammeCode);

            DataverseEntityHelper.MoveOpportunityBusinessProcessFlowToStage(
                service,
                tracingService,
                opportunityReference,
                request.InvestmentProgrammeCode);//BU specific opeartion
            tracingService.Trace(
                "Business process flow stage update completed for opportunity '{0}'.",
                opportunityReference.Id);

            tracingService.Trace(
                "Retrieving opportunity '{0}' values required for KYC request creation.",
                opportunityReference.Id);
            Entity opportunityForKycRequest = RetrieveOpportunityForKycRequest(service, opportunityReference);
            /* tracingService.Trace(
                "Resolving KYC request country lookup from '{0}'.",
                CreateLeadOpportunityKycRequestConstant.DefaultKycCountryName);
            EntityReference countryOfPrincipalPlaceOfBusiness = DataverseEntityHelper.ResolveSingleLookup(
                service,
                CreateLeadOpportunityKycRequestConstant.CountryEntityLogicalName,
                CreateLeadOpportunityKycRequestConstant.CountryPrimaryNameAttributeName,
                CreateLeadOpportunityKycRequestConstant.DefaultKycCountryName,
                "country");
            tracingService.Trace(
                "Resolved country '{0}' to record Id '{1}'.",
                CreateLeadOpportunityKycRequestConstant.DefaultKycCountryName,
                countryOfPrincipalPlaceOfBusiness.Id); */

            tracingService.Trace("Building KYC request entity from opportunity values.");
            Entity kycRequest = BuildKYCRequestEntity(
                opportunityForKycRequest);
            tracingService.Trace(
                "Creating KYC request record with {0} attribute(s).",
                kycRequest.Attributes.Count);
            Guid kycRequestId = service.Create(kycRequest);
            tracingService.Trace("KYC request created with Id: {0}", kycRequestId);
            // MarkOpportunityBpfFinished(service, opportunityReference);
            // CloseOpportunityAsWon(service, opportunityReference, request.Subject);

            context.OutputParameters[CreateLeadOpportunityKycRequestConstant.LeadIdOutputParameterName] = leadId;
            context.OutputParameters[CreateLeadOpportunityKycRequestConstant.OpportunityIdOutputParameterName] =
                opportunityReference.Id;
            context.OutputParameters[CreateLeadOpportunityKycRequestConstant.KycRequestIdOutputParameterName] =
                kycRequestId;
            tracingService.Trace(
                "Custom API output parameters populated with lead Id '{0}' and opportunity Id '{1}'.",
                leadId,
                opportunityReference.Id);
        }

        private static Entity BuildLeadEntity(
            CreateLeadRequest request,
            EntityReference transactionCurrency,
            EntityReference parentAccount,
            EntityReference accountLead,
            EntityReference parentContact,
            EntityReference investmentProgramme,
            EntityReference investmentSubProgramme)
        {
            Entity lead = new Entity("lead");
            lead["subject"] = request.Subject;
            lead["fullname"] = request.Subject;
            lead["bbb_dealname"] = request.DealName;
            lead["companyname"] = parentAccount.Name;
            lead["transactioncurrencyid"] = transactionCurrency;
            lead["parentaccountid"] = parentAccount;
            lead["parentcontactid"] = parentContact;
            lead["bbb_investmentprogramme"] = investmentProgramme;
            lead["bbb_investmentsubprogramme"] = investmentSubProgramme;
            lead["ukn_sector"] = new OptionSetValue(request.SectorValue);
            lead["ukn_initialcontactdate"] = request.InitialContactDate;
            lead["ukn_accountlead"] = accountLead;
            lead["ukn_expectedbbbcommitment"] = request.ExpectedCommitment;

            return lead;
        }

        private static Entity RetrieveOpportunityForKycRequest(
            IOrganizationService service,
            EntityReference opportunityReference)
        {
            return service.Retrieve(
                opportunityReference.LogicalName,
                opportunityReference.Id,
                new ColumnSet(
                    CreateLeadOpportunityKycRequestConstant.OpportunityAccountLeadAttributeName,
                    CreateLeadOpportunityKycRequestConstant.OpportunityProgrammeAttributeName,
                    CreateLeadOpportunityKycRequestConstant.OpportunitySubProgrammeAttributeName,
                    CreateLeadOpportunityKycRequestConstant.OpportunityParentAccountAttributeName));
        }

        private static Entity BuildOpportunityUpdateEntity(
            IOrganizationService service,
            EntityReference opportunityReference,
            UpdateOpportunityRequest request)
        {
            OpportunityAttributeValueResolver attributeValueResolver =
                new OpportunityAttributeValueResolver(service);
            Entity opportunity = new Entity(opportunityReference.LogicalName, opportunityReference.Id);

            (string AttributeName, object Value)[] optionalFields =
            {
                (CreateLeadOpportunityKycRequestConstant.OpportunityTotalCommitmentAttributeName, request.TotalCommitment),
                (CreateLeadOpportunityKycRequestConstant.OpportunityTargetFundSizeAttributeName, request.TargetFundSize),
                (CreateLeadOpportunityKycRequestConstant.OpportunityFundDomicileAttributeName, request.FundDomicileValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityDateQuestionnaireReceivedAttributeName, request.DateQuestionnaireReceived),
                (CreateLeadOpportunityKycRequestConstant.OpportunityIntroMeetingDateAttributeName, request.IntroMeetingDate),
                (CreateLeadOpportunityKycRequestConstant.OpportunityPitchMeetingDateAttributeName, request.PitchMeetingDate),
                (CreateLeadOpportunityKycRequestConstant.OpportunityBpcUkContentRequirementAttributeName, request.BpcUkContentRequirement),
                (CreateLeadOpportunityKycRequestConstant.OpportunityBpcSectorsAttributeName, request.BpcSectors),
                (CreateLeadOpportunityKycRequestConstant.OpportunityIcCommitteeDateAttributeName, request.IcCommitteeDate),
                (CreateLeadOpportunityKycRequestConstant.OpportunityCompletionExpectedAttributeName, request.CompletionExpected),
                (CreateLeadOpportunityKycRequestConstant.OpportunityIcCommitteeApprovedAttributeName, request.IcCommitteeApproved),
            };

            foreach ((string AttributeName, object Value) optionalField in optionalFields)
            {
                DataverseEntityHelper.AddAttributeIfPresent(
                    opportunity,
                    optionalField.AttributeName,
                    optionalField.Value,
                    attributeValueResolver);
            }

            (string AttributeName, object Value)[] requiredFields =
            {
                (CreateLeadOpportunityKycRequestConstant.OpportunityEoiReceivedAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityWipDiscussionAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityReceivedCompletedQuestionnaireAttributeName, CreateLeadOpportunityKycRequestConstant.YesAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityInvestmentSummaryNoteAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityPrePitchPaperAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityTeamDecisionAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityDirectorApprovalAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityFeedbackProvidedAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityOnsiteMeetingAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityReferencingDoneAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityDdReportAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityIcPaperDatesAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
                (CreateLeadOpportunityKycRequestConstant.OpportunityIcCommitteeAttributeName, CreateLeadOpportunityKycRequestConstant.CompletedAttributeValue),
            };

            foreach ((string AttributeName, object Value) requiredField in requiredFields)
            {
                attributeValueResolver.SetAttribute(
                    opportunity,
                    requiredField.AttributeName,
                    requiredField.Value);
            }

            return opportunity;
        }

        private static Entity BuildKYCRequestEntity(
            Entity opportunity)
        {
            Entity kycRequest = new Entity(CreateLeadOpportunityKycRequestConstant.KycRequestEntityLogicalName);
            kycRequest[CreateLeadOpportunityKycRequestConstant.KycRequestRelationshipLeadAttributeName] =
                DataverseEntityHelper.GetRequiredEntityReferenceAttribute(
                opportunity,
                CreateLeadOpportunityKycRequestConstant.OpportunityAccountLeadAttributeName);
            kycRequest[CreateLeadOpportunityKycRequestConstant.KycRequestProgrammeAttributeName] =
                DataverseEntityHelper.GetRequiredEntityReferenceAttribute(
                opportunity,
                CreateLeadOpportunityKycRequestConstant.OpportunityProgrammeAttributeName);
            kycRequest[CreateLeadOpportunityKycRequestConstant.KycRequestDeliveryPartnerAttributeName] =
                DataverseEntityHelper.GetRequiredEntityReferenceAttribute(
                opportunity,
                CreateLeadOpportunityKycRequestConstant.OpportunityParentAccountAttributeName);
            kycRequest[CreateLeadOpportunityKycRequestConstant.KycRequestOriginatingOpportunityAttributeName] =
                opportunity.ToEntityReference();
            /* kycRequest[
                CreateLeadOpportunityKycRequestConstant.KycRequestCountryOfPrincipalPlaceOfBusinessAttributeName] =
                countryOfPrincipalPlaceOfBusiness; */

            EntityReference subProgramme = opportunity.GetAttributeValue<EntityReference>(
                CreateLeadOpportunityKycRequestConstant.OpportunitySubProgrammeAttributeName);
            if (subProgramme != null)
            {
                kycRequest[CreateLeadOpportunityKycRequestConstant.KycRequestSubProgrammeAttributeName] =
                    subProgramme;
            }

            return kycRequest;
        }

        private static void CloseOpportunityAsWon(
            IOrganizationService service,
            EntityReference opportunityReference,
            string subject)
        {
            Entity opportunityClose = new Entity("opportunityclose");
            opportunityClose["opportunityid"] = opportunityReference;
            opportunityClose["subject"] = string.Format(
                CultureInfo.InvariantCulture,
                "Opportunity won for {0}",
                subject);
            opportunityClose["actualend"] = DateTime.UtcNow;

            WinOpportunityRequest winOpportunityRequest = new WinOpportunityRequest
            {
                OpportunityClose = opportunityClose,
                Status = new OptionSetValue(CreateLeadOpportunityKycRequestConstant.WonOpportunityStatusCode)
            };

            service.Execute(winOpportunityRequest);
        }

        private static void MarkOpportunityBpfFinished(
            IOrganizationService service,
            EntityReference opportunityReference)
        {
            Entity opportunity = new Entity(opportunityReference.LogicalName, opportunityReference.Id);
            opportunity[CreateLeadOpportunityKycRequestConstant.BpfFinishedAttributeName] = true;
            service.Update(opportunity);
        }

    }
}
