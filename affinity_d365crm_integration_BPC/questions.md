# Integration Questions

## Current Flow for all BUs

Lead creation → Qualify to opportunity → Create and complete KYC (to close opportunity) → Close opportunity → Facility/Initial close gets created

## Current Flow for BPC direct programme (BPC core co-investment or future fund breakthrough)

Lead creation → Qualify to opportunity → Create and complete KYC (to close opportunity can happen anytime but before closure) → Facility/Initial close gets created ( when moving from stage due diligence to execution stage) → Close opportunity

## My Understanding of the Affinity Deal Process
 
Lead gets created in Affinity → Qualify to Opportunity → Send details to D365 CRM for KYC completion, where Lead, Opportunity, and KYC are created in D365 CRM → Once KYC is completed in D365 CRM, confirmation is sent back to Affinity → Opportunity gets closed in Affinity

## Questions for Integration

1. **In opportunity**
   - When will the opportunity be closed in D365 CRM?

2. **KYC Request Timing**
   - Do we need to close the opportunity as won before creating the KYC request?
   - As per our initial discussion, KYC request will be manually completed by the user.
   - If this is the case, when does facility/initial close creation happen?

3. **Business Unit Scope**
   - Currently we are starting with BPC, will there be changes for each business unit or are we making it generic across all business units?
   - Also is this change only for BPC Direct or Non-direct or both ( facility creation trigger differs for both )
   - Programmatically created lead and opportunity will be editable by users in D365 CRM or is it just for integration purpose? This will have an impact on application level.

4. **Static Reference Data**
   - Account, Contact, Programme, Sub-Programme, Sub-Asset Class, users, currency and Funding Vehicle. How this will get referenced in D365 CRM while creating lead and opportunity? Without these data lead and opportunity cannot be created in D365 CRM.



