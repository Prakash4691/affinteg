using Microsoft.Xrm.Sdk;
using System;

namespace custom_api_plugin
{
    internal sealed class CreateLeadRequest
    {
        private const string SubjectParameterName = "ukn_subject";
        private const string CurrencyParameterName = "ukn_transactioncurrencyid";
        private const string ParentAccountParameterName = "ukn_parentaccount";
        private const string AccountLeadParameterName = "ukn_accountlead";
        private const string DealNameParameterName = "ukn_dealname";
        private const string InitialContactDateParameterName = "ukn_initialcontactdate";
        private const string ParentContactParameterName = "ukn_parentcontact";
        private const string SectorParameterName = "ukn_sector";
        private const string InvestmentProgrammeParameterName = "ukn_investmentprogramme";
        private const string InvestmentSubProgrammeParameterName = "ukn_investmentsubprogramme";
        private const string ExpectedCommitmentParameterName = "ukn_expectedbbbcommitment";
        public string Subject { get; private set; }

        public string CurrencyInput { get; private set; }

        public string ParentAccountId { get; private set; }

        public string AccountLeadEmailAddress { get; private set; }

        public string DealName { get; private set; }

        public DateTime InitialContactDate { get; private set; }

        public string ParentContactId { get; private set; }

        public int SectorValue { get; private set; }

        public string InvestmentProgrammeCode { get; private set; }

        public string InvestmentSubProgrammeCode { get; private set; }

        public Money ExpectedCommitment { get; private set; }

        public static CreateLeadRequest FromContext(IPluginExecutionContext context)
        {
            if (context == null)
            {
                throw new InvalidPluginExecutionException("The plug-in execution context is required.");
            }

            return new CreateLeadRequest
            {
                Subject = CustomApiRequestParameterReader.GetRequiredStringParameter(context, SubjectParameterName),
                CurrencyInput = CustomApiRequestParameterReader.GetRequiredStringParameter(context, CurrencyParameterName),
                ParentAccountId = CustomApiRequestParameterReader.GetRequiredStringParameter(context, ParentAccountParameterName),
                AccountLeadEmailAddress = CustomApiRequestParameterReader.GetRequiredStringParameter(context, AccountLeadParameterName),
                DealName = CustomApiRequestParameterReader.GetRequiredStringParameter(context, DealNameParameterName),
                InitialContactDate = CustomApiRequestParameterReader.GetRequiredDateParameter(context, InitialContactDateParameterName),
                ParentContactId = CustomApiRequestParameterReader.GetRequiredStringParameter(context, ParentContactParameterName),
                SectorValue = CustomApiRequestParameterReader.GetRequiredIntegerParameter(context, SectorParameterName),
                InvestmentProgrammeCode = CustomApiRequestParameterReader.GetRequiredStringParameter(context, InvestmentProgrammeParameterName),
                InvestmentSubProgrammeCode = CustomApiRequestParameterReader.GetOptionalStringParameter(context, InvestmentSubProgrammeParameterName),
                ExpectedCommitment = CustomApiRequestParameterReader.GetRequiredMoneyParameter(context, ExpectedCommitmentParameterName)
            };
        }
    }
}
