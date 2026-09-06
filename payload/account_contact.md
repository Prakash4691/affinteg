# Account Contact API

## Request Structure

### Account Fields
- `ukn_name`: Account name (e.g., "Acme Ventures Ltd 32")
- `ukn_relationshiplead`: Relationship lead email (must exist in D365 CRM)
- `ukn_address1line1`: Address line 1
- `ukn_address1postalcode`: Postal code

### Contact Fields
- `ukn_emailaddress1`: Contact email address
- `ukn_firstname`: Contact first name
- `ukn_lastname`: Contact last name

### Example Request

```json
{
  "ukn_name": "Acme Ventures Ltd 32",
  "ukn_relationshiplead": "Prakash.Kalimuthu@british-business-bank.co.uk",
  "ukn_address1line1": "10 Bishopsgate",
  "ukn_address1postalcode": "CM1234",
  "ukn_emailaddress1": "tes32@gmail.com",
  "ukn_firstname": "IBM",
  "ukn_lastname": "Tech"
}
```

## Response Structure

The API returns account and contact identifiers in the following format:

```json
{
  "@odata.context": "https://bbb-d365-dev1.crm11.dynamics.com/api/data/v9.2/$metadata#Microsoft.Dynamics.CRM.ukn_AffinityAccountContactResponse",
  "ukn_accountid": "77a53f38-4a80-f111-ab0e-6045bd0b5844",
  "ukn_contactid": "8b29b43e-4a80-f111-ab0e-6045bd0b5844"
}
```

### Response Fields
- `@odata.context`: OData metadata context URL
- `ukn_accountid`: Created or matched account ID (GUID)
- `ukn_contactid`: Created or matched contact ID (GUID)