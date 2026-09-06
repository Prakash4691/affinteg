using Microsoft.Xrm.Sdk;
using System;

namespace custom_api_plugin
{
    internal sealed class UpdateOpportunityRequest
    {
        private const string TotalCommitmentParameterName = "ukn_totalcommitment";
        private const string TargetFundSizeParameterName = "ukn_targetfundsize";
        private const string FundDomicileParameterName = "ukn_funddomicile";
        private const string DateQuestionnaireReceivedParameterName = "ukn_datequestionnairereceived";
        private const string IntroMeetingDateParameterName = "ukn_intromeetingdate";
        private const string PitchMeetingDateParameterName = "ukn_pitchmeetingdate";
        private const string BpcUkContentRequirementParameterName = "ukn_bpcukcontentrequirement";
        private const string BpcSectorsParameterName = "ukn_bpcsectors";
        private const string IcCommitteeDateParameterName = "ukn_iccommitteedate";
        private const string CompletionExpectedParameterName = "ukn_completionexpected";
        private const string IcCommitteeApprovedParameterName = "ukn_iccommitteeapproved";

        public decimal? TotalCommitment { get; private set; }

        public decimal? TargetFundSize { get; private set; }

        public int? FundDomicileValue { get; private set; }

        public DateTime? DateQuestionnaireReceived { get; private set; }

        public DateTime? IntroMeetingDate { get; private set; }

        public DateTime? PitchMeetingDate { get; private set; }

        public decimal? BpcUkContentRequirement { get; private set; }

        public string BpcSectors { get; private set; }

        public DateTime? IcCommitteeDate { get; private set; }

        public DateTime? CompletionExpected { get; private set; }

        public DateTime? IcCommitteeApproved { get; private set; }

        public static UpdateOpportunityRequest FromContext(IPluginExecutionContext context)
        {
            if (context == null)
            {
                throw new InvalidPluginExecutionException("The plug-in execution context is required.");
            }

            return new UpdateOpportunityRequest
            {
                TotalCommitment = CustomApiRequestParameterReader.GetOptionalDecimalParameter(context, TotalCommitmentParameterName),
                TargetFundSize = CustomApiRequestParameterReader.GetOptionalDecimalParameter(context, TargetFundSizeParameterName),
                FundDomicileValue = CustomApiRequestParameterReader.GetOptionalIntegerParameter(context, FundDomicileParameterName),
                DateQuestionnaireReceived = CustomApiRequestParameterReader.GetOptionalDateParameter(context, DateQuestionnaireReceivedParameterName),
                IntroMeetingDate = CustomApiRequestParameterReader.GetOptionalDateParameter(context, IntroMeetingDateParameterName),
                PitchMeetingDate = CustomApiRequestParameterReader.GetOptionalDateParameter(context, PitchMeetingDateParameterName),
                BpcUkContentRequirement = CustomApiRequestParameterReader.GetOptionalDecimalParameter(context, BpcUkContentRequirementParameterName),
                BpcSectors = CustomApiRequestParameterReader.GetOptionalStringParameter(context, BpcSectorsParameterName),
                IcCommitteeDate = CustomApiRequestParameterReader.GetOptionalDateParameter(context, IcCommitteeDateParameterName),
                CompletionExpected = CustomApiRequestParameterReader.GetOptionalDateParameter(context, CompletionExpectedParameterName),
                IcCommitteeApproved = CustomApiRequestParameterReader.GetOptionalDateParameter(context, IcCommitteeApprovedParameterName)
            };
        }
    }
}
