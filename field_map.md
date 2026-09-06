BPC Lead:
subject
parentaccountid (Account-lookup)
parentcontactid (contact-lookup)
bbb_investmentprogramme (Programme-lookup)
bbb_dealname
ukn_sector
ukn_initialcontactdate
ukn_accountlead (User-lookup)
ukn_expectedbbbcommitment
transactioncurrencyid (currency-lookup)

Opportunity:
name (copy from lead)
parentaccountid (copy from lead)
parentcontactid (copy from lead)
transactioncurrencyid (copy from lead)
bbb_investmentprogramme (copy from lead)
ukn_accountlead (copy from lead)
ukn_subassetclass (Sub Asset Class-lookup)
ukn_fundid (Funding Vehicle-lookup)
bbb_bbbcommitment (copy from lead)
ukn_completiondate

KYC request:
ukn_programme (copy from opportunity)
ukn_bbbproductmanagerrelationshiplead (copy from opportunity)
ukn_deliverypartner(Account-copy from opportunity)
ukn_originatingopportunity (Opportunity record itself)
ukn_countryofprincipalplaceofbusiness(Country-lookup)

Facility:
ukn_name (copy from opportunity)
ukn_account (copy from opportunity)
ukn_completiondate (copy from opportunity)
transactioncurrencyid (copy from opportunity)
ukn_bbbcommitment (copy from opportunity)
ukn_currenttotalcommitment (copy from opportunity)
Do we need dryclose, date fields or first second contact values?
ukn_fundingvehicle (copy from opportunity)
ukn_maximumbbbcommitmenticapproved (copy from opportunity)
ukn_opportunitycurrency (same as transactioncurrencyid)
ukn_originalcommitment (copy from opportunity)
ukn_investmentprogramme (copy from opportunity)
ukn_investmentsubprogramme (copy from opportunity)
ownerid (copy from opportunity owner)

FacilityOpportunity:
type as initial, associating opportunity and facility to complete the process