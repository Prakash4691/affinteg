# BPC integration prerequisite lookup/reference tables

These lookup/reference tables should be available in Affinity before the BPC integration starts so lead, opportunity, KYC request, and facility records can resolve their required lookups.

| Table / entity | Needed by | Why it is needed | Mandatory before go-live |
| --- | --- | --- | --- |
| Account | Lead, Opportunity, Facility, KYC Request | Used by `parentaccountid`; also company/account context is carried forward into opportunity and facility. KYC request `ukn_deliverypartner` also points to the Account table, so delivery partner accounts must exist there too. | Yes |
| Contact | Lead, Opportunity | Used by `parentcontactid` on lead and opportunity. | Yes |
| Investment Programme (`bbb_investmentprogrammes`) | Lead, Opportunity, Facility, KYC Request | Used by `bbb_investmentprogramme` / programme lookups and copied through to facility and KYC request. | Yes |
| Investment Sub-Programme (`bbb_investmentsubprogrammes`) | Lead, Opportunity, Facility, KYC Request | Used by `bbb_investmentsubprogramme` / sub-programme lookups. Optional on lead/KYC request, but required for facility creation when used through opportunity. | Conditional |
| System User | Lead, Opportunity, Facility, KYC Request | Used for `ukn_accountlead`, opportunity owner / relationship lead, and KYC product manager relationship lead. | Yes |
| Transaction Currency | Lead, Opportunity, Facility | Used by `transactioncurrencyid` and facility opportunity currency. | Yes |
| Funding Vehicle (`ukn_funds`) | Opportunity, Facility | Used by `ukn_fundid` on opportunity and copied to facility as funding vehicle. | Yes |
| Sub-Asset Class | Opportunity | Used by `ukn_subassetclass` on opportunity. | Yes |
| Country | KYC Request | Used by `ukn_countryofprincipalplaceofbusiness`. | Yes |

## Notes

- `Sector`, `Dry Close`, `Top Up Existing Facility`, and similar option set values are not lookup tables, so they are not included here.
- Transactional tables such as Lead, Opportunity, KYC Request, and Facility are not prerequisite lookup migrations; they are the records created or updated by the integration itself.
- If Affinity needs strict ID-based matching rather than name-based matching, the migrated reference data must preserve the integration key used for each lookup.