using Microsoft.Xrm.Sdk;
using System;

namespace custom_api_plugin
{
    internal sealed class CreateAccountContactRequest
    {
        private const string AccountNameParameterName = "ukn_name";
        private const string RelationshipLeadParameterName = "ukn_relationshiplead";
        private const string AccountAddressLine1ParameterName = "ukn_address1line1";
        private const string AccountPostalCodeParameterName = "ukn_address1postalcode";
        private const string ContactEmailAddressParameterName = "ukn_emailaddress1";
        private const string ContactFirstNameParameterName = "ukn_firstname";
        private const string ContactLastNameParameterName = "ukn_lastname";
        private const string AffinityAccountIdParameterName = "ukn_affinityaccountid";

        public string AccountName { get; private set; }

        public string RelationshipLeadEmailAddress { get; private set; }

        public string AccountAddressLine1 { get; private set; }

        public string AccountPostalCode { get; private set; }

        public string ContactEmailAddress { get; private set; }

        public string ContactFirstName { get; private set; }

        public string ContactLastName { get; private set; }

        public string AffinityAccountId { get; private set; }

        public static CreateAccountContactRequest FromContext(IPluginExecutionContext context)
        {
            if (context == null)
            {
                throw new InvalidPluginExecutionException("The plug-in execution context is required.");
            }

            return new CreateAccountContactRequest
            {
                AccountName = CustomApiRequestParameterReader.GetRequiredStringParameter(context, AccountNameParameterName),
                RelationshipLeadEmailAddress = CustomApiRequestParameterReader.GetRequiredStringParameter(context, RelationshipLeadParameterName),
                AccountAddressLine1 = CustomApiRequestParameterReader.GetRequiredStringParameter(context, AccountAddressLine1ParameterName),
                AccountPostalCode = CustomApiRequestParameterReader.GetRequiredStringParameter(context, AccountPostalCodeParameterName),
                ContactEmailAddress = CustomApiRequestParameterReader.GetRequiredStringParameter(context, ContactEmailAddressParameterName),
                ContactFirstName = CustomApiRequestParameterReader.GetOptionalStringParameter(context, ContactFirstNameParameterName),
                ContactLastName = CustomApiRequestParameterReader.GetRequiredStringParameter(context, ContactLastNameParameterName),
                AffinityAccountId = CustomApiRequestParameterReader.GetRequiredStringParameter(context, AffinityAccountIdParameterName)
            };
        }
    }
}
