using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace custom_api_plugin
{
    public class CreateAccountContact : PluginBase
    {
        private const string AccountIdOutputParameterName = "ukn_accountid";
        private const string ContactIdOutputParameterName = "ukn_contactid";
        private const string AccountCreatedOrUpdatedOutputParameterName = "ukn_accountcreatedorupdated";

        public CreateAccountContact(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(CreateAccountContact))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new InvalidPluginExecutionException("The local plug-in context is required.");
            }

            ExecuteCreateAccountContact(localPluginContext);
        }

        private static void ExecuteCreateAccountContact(ILocalPluginContext localPluginContext)
        {
            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;
            IOrganizationService service = localPluginContext.InitiatingUserService;
            ITracingService tracingService = localPluginContext.TracingService;

            tracingService.Trace("Reading and validating Custom API request parameters for account/contact creation.");
            CreateAccountContactRequest request = CreateAccountContactRequest.FromContext(context);

            tracingService.Trace("Creating account and contact from Custom API input parameters.");
            tracingService.Trace("Validated request payload for account and contact creation.");

            tracingService.Trace("Resolving relationship lead from provided internal email address.");
            EntityReference relationshipLead = DataverseEntityHelper.ResolveSingleLookup(
                service,
                "systemuser",
                "internalemailaddress",
                request.RelationshipLeadEmailAddress,
                "relationship lead");
            tracingService.Trace("Resolved relationship lead to record Id '{0}'.", relationshipLead.Id);

            tracingService.Trace("Building account entity from validated request data.");
            Entity account = BuildAccountEntity(request, relationshipLead);
            tracingService.Trace("Creating account record in Dataverse.");
            var response = (UpsertResponse)service.Execute(new UpsertRequest { Target = account });
            Guid accountId = response.Target.Id;
            string accountStatus = response.RecordCreated ? "created" : "updated";
            tracingService.Trace("Account {0} with Id: {1}", accountStatus, accountId);

            tracingService.Trace("Building contact entity from validated request data.");
            Entity contact = BuildContactEntity(request);
            tracingService.Trace("Creating contact record in Dataverse.");
            Guid contactId = service.Create(contact);
            tracingService.Trace("Contact created with Id: {0}", contactId);

            context.OutputParameters[AccountIdOutputParameterName] = accountId;
            context.OutputParameters[AccountCreatedOrUpdatedOutputParameterName] = accountStatus;
            context.OutputParameters[ContactIdOutputParameterName] = contactId;
            tracingService.Trace(
                "Custom API output parameters populated with account Id '{0}' and contact Id '{1}'.",
                accountId,
                contactId);
        }

        private static Entity BuildAccountEntity(
            CreateAccountContactRequest request,
            EntityReference relationshipLead)
        {
            Entity account = new Entity("account", "ukn_affinityaccountid", request.AffinityAccountId);
            account["name"] = request.AccountName;
            account["ukn_relationshiplead"] = relationshipLead;
            account["address1_line1"] = request.AccountAddressLine1;
            account["address1_postalcode"] = request.AccountPostalCode;
            return account;
        }

        private static Entity BuildContactEntity(CreateAccountContactRequest request)
        {
            Entity contact = new Entity("contact");
            contact["lastname"] = request.ContactLastName;
            contact["emailaddress1"] = request.ContactEmailAddress;

            if (!string.IsNullOrWhiteSpace(request.ContactFirstName))
            {
                contact["firstname"] = request.ContactFirstName;
            }

            return contact;
        }

    }
}