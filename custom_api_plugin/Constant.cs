using System;

namespace custom_api_plugin
{
    internal static class Constant
    {
        internal static class CreateLeadOpportunityKYCRequestValues
        {
            internal const int QualifiedLeadStatusCode = 3;
            internal const int WonOpportunityStatusCode = 3;
            internal const string LeadIdOutputParameterName = "ukn_leadid";
            internal const string OpportunityIdOutputParameterName = "ukn_opportunityid";
            internal const string KycRequestIdOutputParameterName = "ukn_kycrequestid";
            internal const string BpfFinishedAttributeName = "ukn_bpffinished";
            internal const string OpportunityEntityLogicalName = "opportunity";
            internal const string KycRequestEntityLogicalName = "ukn_kycrequest";
            internal const string CountryEntityLogicalName = "ukn_country";
            internal const string CountryPrimaryNameAttributeName = "ukn_name";
            internal const string DefaultKycCountryName = "United Kingdom (GB)";
            internal const string CompletedAttributeValue = "completed";
            internal const string YesAttributeValue = "yes";
            internal const string OpportunityAccountLeadAttributeName = "ukn_accountlead";
            internal const string OpportunityProgrammeAttributeName = "bbb_investmentprogramme";
            internal const string OpportunitySubProgrammeAttributeName = "bbb_investmentsubprogramme";
            internal const string OpportunityParentAccountAttributeName = "parentaccountid";
            internal const string OpportunityTotalCommitmentAttributeName = "bbb_totalcommitment";
            internal const string OpportunityTargetFundSizeAttributeName = "ukn_targetfundsize";
            internal const string OpportunityEoiReceivedAttributeName = "bbb_eoireceived";
            internal const string OpportunityWipDiscussionAttributeName = "ukn_wipdiscussion";
            internal const string OpportunityFundDomicileAttributeName = "ukn_funddomicile";
            internal const string OpportunityReceivedCompletedQuestionnaireAttributeName =
                "ukn_receivedcompletedquestionnaire";
            internal const string OpportunityDateQuestionnaireReceivedAttributeName = "ukn_datequestionnairereceived";
            internal const string OpportunityIntroMeetingDateAttributeName = "ukn_intromeetingdate";
            internal const string OpportunityInvestmentSummaryNoteAttributeName = "ukn_investmentsummarynote";
            internal const string OpportunityPrePitchPaperAttributeName = "ukn_prepitchpaper";
            internal const string OpportunityPitchMeetingDateAttributeName = "ukn_pitchmeetingdate";
            internal const string OpportunityTeamDecisionAttributeName = "ukn_teamdecision";
            internal const string OpportunityDirectorApprovalAttributeName = "ukn_directorapproval";
            internal const string OpportunityFeedbackProvidedAttributeName = "ukn_feedbackprovided";
            internal const string OpportunityBpcUkContentRequirementAttributeName = "ukn_bpcukcontentrequirement";
            internal const string OpportunityBpcSectorsAttributeName = "ukn_bpcsectors";
            internal const string OpportunityIcCommitteeDateAttributeName = "ukn_iccommitteedate";
            internal const string OpportunityOnsiteMeetingAttributeName = "ukn_onsitemeeting";
            internal const string OpportunityReferencingDoneAttributeName = "ukn_referencingdone";
            internal const string OpportunityDdReportAttributeName = "bbb_ddreport";
            internal const string OpportunityIcPaperDatesAttributeName = "ukn_icpaperdates";
            internal const string OpportunityIcCommitteeAttributeName = "ukn_iccommittee";
            internal const string OpportunityCompletionExpectedAttributeName = "bbb_completionexpected";
            internal const string OpportunityIcCommitteeApprovedAttributeName = "ukn_iccommitteeapproved";
            internal const string KycRequestRelationshipLeadAttributeName = "ukn_bbbproductmanagerrelationshiplead";
            internal const string KycRequestProgrammeAttributeName = "ukn_programme";
            internal const string KycRequestSubProgrammeAttributeName = "ukn_subprogramme";
            internal const string KycRequestDeliveryPartnerAttributeName = "ukn_deliverypartner";
            internal const string KycRequestOriginatingOpportunityAttributeName = "ukn_originatingopportunity";
            internal const string KycRequestCountryOfPrincipalPlaceOfBusinessAttributeName =
                "ukn_countryofprincipalplaceofbusiness";
            internal const string InvestmentProgrammeCodeBPCCoreFund = "BPC200";
            internal const string ProcessStageEntityLogicalName = "processstage";
            internal const string WorkflowEntityLogicalName = "workflow";
            internal const string ActiveStageIdAttributeName = "activestageid";
            internal const string ProcessStageIdAttributeName = "processstageid";
            internal const string ProcessIdAttributeName = "processid";
            internal const string TraversedPathAttributeName = "traversedpath";
            internal const string WorkflowUniqueNameAttributeName = "uniquename";
            internal static readonly Guid BPCDirectLeadBpfMarketEngagementStageId =
                new Guid("bd7cfe76-0b6e-49be-a6d9-0837b735c22f");
            internal static readonly Guid BPCDirectOpportunityBpfEoiStageId =
                new Guid("f34ca941-1136-440d-8e4d-8026549c75be");
            internal static readonly Guid BPCDirectOpportunityBpfFormalProposalBpcCoreStageId =
                new Guid("6a67d811-fc9e-45ff-8bca-95133f9fb764");
            internal static readonly Guid BPCDirectOpportunityBpfFormalProposalNonBpcCoreStageId =
                new Guid("c4799b07-e73e-451b-98cb-79d02d4bb327");
            internal static readonly Guid BPCDirectOpportunityBpfDdStageId =
                new Guid("14749817-5f3c-4096-8b14-5f1c029b8a7d");
        }
    }
}
