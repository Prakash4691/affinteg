# custom_api_plugin

Dataverse plug-in assembly for the Affinity to Dynamics 365 CRM integration. The project targets **.NET Framework 4.6.2** and contains the plug-in entry points that create and enrich CRM records from Affinity payloads.

## Project contents

| Class / file | Purpose |
| --- | --- |
| `CreateAccountContact.cs` | Creates an `account` and a `contact` from discrete Custom API parameters. |
| `CreateLeadOpportunityKYCRequest.cs` | Creates a `lead`, qualifies it to an `opportunity`, updates opportunity/BPF fields, and creates a `ukn_kycrequest`. |
| `BPCDirectAffinity.cs` | Legacy JSON-payload entry point that creates a `lead`, qualifies it to an `opportunity`, and applies a JSON-driven opportunity update. It does **not** create a KYC request. |
| `CreateAccountContactRequest.cs`, `CreateLeadRequest.cs`, `UpdateOpportunityRequest.cs` | Strongly typed request readers for the current parameter-based contracts. |
| `CustomApiRequestParameterReader.cs`, `CustomApiDateParser.cs` | Shared validation and parsing for strings, numbers, money, and dates. |
| `DataverseEntityHelper.cs`, `OpportunityAttributeValueResolver.cs` | Shared lookup resolution and metadata-based attribute conversion helpers. |
| `PluginBase.cs` | Common plug-in execution, tracing, and error handling wrapper. |

## Build

Build the assembly from this folder with:

```powershell
dotnet build custom_api_plugin.csproj
```

The project uses `Microsoft.PowerApps.MSBuild.Plugin` packaging targets and signs the assembly with `affinitycrmintegration.snk`.

## Shared runtime behavior

- Lookup resolution is strict. If a lookup value resolves to zero records or more than one record, the plug-in throws an `InvalidPluginExecutionException`.
- Transaction currency input is normalized before lookup. Supported aliases are `usd`, `us dollar`, `eur`, `euro`, `gbp`, `sterling`, and `pound sterling`.
- Supported date formats are `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm:ss`, `yyyy-MM-ddTHH:mm:ss.fff`, and round-trip `o`.
- Numeric parameters can generally be provided as numbers or numeric strings.
- Errors are surfaced as plug-in exceptions with trace-friendly detail from `PluginBase`.

## Current parameter-based contracts

### 1. Account and contact creation

Implemented by `CreateAccountContact`.

#### Input parameters

| Parameter | Required | Target field |
| --- | --- | --- |
| `ukn_name` | Yes | `account.name` |
| `ukn_relationshiplead` | Yes | `account.ukn_relationshiplead` via `systemuser.internalemailaddress` |
| `ukn_address1line1` | Yes | `account.address1_line1` |
| `ukn_address1postalcode` | Yes | `account.address1_postalcode` |
| `ukn_emailaddress1` | Yes | `contact.emailaddress1` |
| `ukn_firstname` | No | `contact.firstname` |
| `ukn_lastname` | Yes | `contact.lastname` |

#### Example request

```json
{
  "ukn_name": "Acme Ventures Ltd",
  "ukn_relationshiplead": "relationship.lead@british-business-bank.co.uk",
  "ukn_address1line1": "41 Bishopsgate",
  "ukn_address1postalcode": "CM1242",
  "ukn_emailaddress1": "contact@acmeventures.com",
  "ukn_firstname": "Ava",
  "ukn_lastname": "Brown"
}
```

#### Output parameters

```json
{
  "ukn_accountid": "00000000-0000-0000-0000-000000000001",
  "ukn_contactid": "00000000-0000-0000-0000-000000000002"
}
```

#### Notes

- The relationship lead is resolved from `systemuser.internalemailaddress`.
- The current implementation creates the account and contact as separate records. It does **not** set a contact-to-account relationship.

### 2. Lead, opportunity, and KYC request creation

Implemented by `CreateLeadOpportunityKYCRequest`.

#### Required input parameters

| Parameter | Target / meaning |
| --- | --- |
| `ukn_subject` | `lead.subject` and `lead.fullname` |
| `ukn_transactioncurrencyid` | Currency lookup resolved from ISO code or alias |
| `ukn_parentaccount` | Existing `account.name` |
| `ukn_accountlead` | Existing `systemuser.internalemailaddress` |
| `ukn_dealname` | `lead.bbb_dealname` |
| `ukn_initialcontactdate` | `lead.ukn_initialcontactdate` |
| `ukn_parentcontact` | Existing `contact.emailaddress1` |
| `ukn_sector` | `lead.ukn_sector` option set value |
| `ukn_investmentprogramme` | Existing `bbb_investmentprogramme.ukn_programmecodereference` |
| `ukn_investmentsubprogramme` | Existing `bbb_investmentsubprogramme.ukn_investmentsubprogrammecategorycode` |
| `ukn_expectedbbbcommitment` | `lead.ukn_expectedbbbcommitment` |

#### Optional opportunity update parameters

| Parameter | Opportunity field |
| --- | --- |
| `ukn_totalcommitment` | `bbb_totalcommitment` |
| `ukn_targetfundsize` | `ukn_targetfundsize` |
| `ukn_funddomicile` | `ukn_funddomicile` |
| `ukn_datequestionnairereceived` | `ukn_datequestionnairereceived` |
| `ukn_intromeetingdate` | `ukn_intromeetingdate` |
| `ukn_pitchmeetingdate` | `ukn_pitchmeetingdate` |
| `ukn_bpcukcontentrequirement` | `ukn_bpcukcontentrequirement` |
| `ukn_bpcsectors` | `ukn_bpcsectors` |
| `ukn_iccommitteedate` | `ukn_iccommitteedate` |
| `ukn_completionexpected` | `bbb_completionexpected` |
| `ukn_iccommitteeapproved` | `ukn_iccommitteeapproved` |

#### Example request

```json
{
  "ukn_subject": "Affinity deal - direct fund",
  "ukn_transactioncurrencyid": "USD",
  "ukn_parentaccount": "Acme Ventures Ltd",
  "ukn_accountlead": "relationship.lead@british-business-bank.co.uk",
  "ukn_dealname": "Affinity deal - direct fund",
  "ukn_initialcontactdate": "2025-09-14",
  "ukn_parentcontact": "contact@acmeventures.com",
  "ukn_sector": 968200000,
  "ukn_investmentprogramme": "BPC200",
  "ukn_investmentsubprogramme": "FG001",
  "ukn_expectedbbbcommitment": 20,
  "ukn_totalcommitment": 50,
  "ukn_targetfundsize": 200,
  "ukn_funddomicile": 968200000,
  "ukn_datequestionnairereceived": "2025-02-14",
  "ukn_intromeetingdate": "2025-03-14",
  "ukn_pitchmeetingdate": "2025-10-14",
  "ukn_bpcukcontentrequirement": 50,
  "ukn_bpcsectors": "Business Services",
  "ukn_iccommitteedate": "2025-07-14",
  "ukn_completionexpected": "2025-07-14",
  "ukn_iccommitteeapproved": "2025-07-14"
}
```

#### Output parameters

```json
{
  "ukn_leadid": "00000000-0000-0000-0000-000000000003",
  "ukn_opportunityid": "00000000-0000-0000-0000-000000000004"
}
```

#### What this plug-in does

1. Resolves the required existing account, contact, system user, currency, investment programme, and investment sub-programme.
2. Creates a `lead`.
3. Qualifies the lead to create an `opportunity`.
4. Updates the opportunity with any optional input fields supplied.
5. Automatically sets these opportunity/BPF fields to fixed values:
   `bbb_eoireceived`, `ukn_wipdiscussion`, `ukn_investmentsummarynote`, `ukn_prepitchpaper`, `ukn_teamdecision`, `ukn_directorapproval`, `ukn_feedbackprovided`, `ukn_onsitemeeting`, `ukn_referencingdone`, `bbb_ddreport`, `ukn_icpaperdates`, and `ukn_iccommittee` to `completed`, and `ukn_receivedcompletedquestionnaire` to `yes`.
6. Creates a `ukn_kycrequest` using values copied from the created opportunity:
   `ukn_bbbproductmanagerrelationshiplead`, `ukn_programme`, optional `ukn_subprogramme`, `ukn_deliverypartner`, and `ukn_originatingopportunity`.

#### Notes

- The current implementation does **not** output the created KYC request id.
- `ukn_countryofprincipalplaceofbusiness` is **not** populated by the current code. The placeholder country resolution is present but commented out.
- `ukn_bpffinished` is not set and the opportunity is not automatically won/closed; both code paths are currently commented out.

## Legacy JSON contract

Implemented by `BPCDirectAffinity`.

This entry point expects two JSON string parameters:

| Parameter | Meaning |
| --- | --- |
| `ukn_lead` | JSON object used to create the lead |
| `ukn_opportunity` | JSON object used to update the opportunity after lead qualification |

### Required lead payload fields

The `ukn_lead` JSON must contain:

- `ukn_subject`
- `ukn_transactioncurrencyid`
- `ukn_parentaccount`
- `ukn_accountlead`
- `ukn_dealname`
- `ukn_initialcontactdate`
- `ukn_parentcontact`
- `ukn_sector`
- `ukn_investmentprogramme`
- `ukn_expectedbbbcommitment`

### Opportunity payload behavior

- The `ukn_opportunity` JSON can contain any updatable opportunity attributes except `opportunityid` and `@odata.type`, which are ignored.
- Lead payload attributes used for integration control are also ignored during generic copy: `ukn_subject`, `ukn_transactioncurrencyid`, `ukn_parentaccount`, `ukn_accountlead`, `ukn_dealname`, `ukn_initialcontactdate`, `ukn_parentcontact`, `ukn_sector`, `ukn_investmentprogramme`, `ukn_expectedbbbcommitment`, `leadid`, and `@odata.type`.
- Output parameters are the same as the typed lead/opportunity flow: `ukn_leadid` and `ukn_opportunityid`.

### Lookup values inside JSON payloads

The metadata-driven resolver in `BPCDirectAffinity` accepts lookup values in any of these forms:

1. A plain string value, resolved against the target table primary name.
2. A GUID string.
3. An object with `id`.
4. An object with `value`, plus optional `targetEntityLogicalName` and `filterAttributeName`.

This legacy flow creates a lead and opportunity only. It does **not** create a KYC request.
