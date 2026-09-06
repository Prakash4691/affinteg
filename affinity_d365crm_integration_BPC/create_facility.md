# Create Facility Record (Only BPC)

Flow: `Create Facility Record when Opportunity Won`  
Action: `Create_Facility_Record_(Only_BPC)` in `BBBDevPowerAutomateFlowSolution\Workflows\CreateFacilityRecordwhenOpportunityWon-C9E2C8A1-8775-EA11-A811-000D3A86D535.json`

| Facility field | Facility logical name | Source field / expression used | Source table |
| --- | --- | --- | --- |
| Topic | `ukn_name` | `opportunity.name` | Opportunity |
| Account | `ukn_account` | `Get_Account.body/accountid` using `opportunity._parentaccountid_value` | Account |
| Completion Date | `ukn_completiondate` | `opportunity.ukn_completiondate` | Opportunity |
| Currency | `transactioncurrencyid` | `opportunity._transactioncurrencyid_value` bound as `/transactioncurrencies/{id}` | Opportunity lookup to Transaction Currency |
| Current BBB Commitment | `ukn_bbbcommitment` | `variables('Current BBB Commitment')`, which this flow sets from `opportunity.bbb_bbbcommitment` | Opportunity |
| Current Total Commitment | `ukn_currenttotalcommitment` | `opportunity.bbb_totalcommitment` | Opportunity |
| Dry Close | `ukn_dryclose` | `opportunity.ukn_dryclose` | Opportunity |
| Dry Close Expiry Date | `ukn_drycloseexpirydate` | `opportunity.ukn_drycloseexpirydate` | Opportunity |
| End of Investment Period | `ukn_endofinvestmentperiod` | `opportunity.ukn_firstexpirydate` | Opportunity |
| Final Close Deadline Date | `ukn_finalclosedeadlinedate` | `variables('varFinalClosedDate')`, populated from `opportunity.ukn_finalclosedeadlinedate` in `Get_Opp` after the flow's timezone/date conversion | Opportunity |
| First Contact | `ukn_firstcontact` | `opportunity._ukn_accountlead_value` bound as `/systemusers({id})` | Opportunity lookup to System User |
| Funding Vehicle | `ukn_fundingvehicle` | `opportunity._ukn_fundid_value` bound as `/ukn_funds({id})` | Opportunity lookup to Funding Vehicle (`ukn_funds`) |
| Maximum BBB Commitment (IC Approved) | `ukn_maximumbbbcommitmenticapproved` | `opportunity.ukn_maximumbbbcommitment` | Opportunity |
| Number of Days | `ukn_numberofdays` | `opportunity.ukn_daysremaining` | Opportunity |
| Opportunity Currency | `ukn_opportunitycurrency` | `opportunity._transactioncurrencyid_value` bound as `/transactioncurrencies/{id}` | Opportunity lookup to Transaction Currency |
| Original BBB Commitment | `ukn_originalcommitment` | `opportunity.bbb_bbbcommitment` | Opportunity |
| Programme | `ukn_investmentprogramme` | `Get_Investment_Programmes.body/@odata.id` using `opportunity._bbb_investmentprogramme_value` | Investment Programme |
| Relationship Lead | `ownerid` | `opportunity._ownerid_value` bound as `/systemusers/{id}` | Opportunity lookup to System User |
| Sub-Programme | `ukn_investmentsubprogramme` | `Get_Sub-Programme.body/@odata.id` using `opportunity._bbb_investmentsubprogramme_value` | Investment Sub-Programme |

## Supporting actions used by this create step

| Action | What it reads |
| --- | --- |
| `Get_Account` | Reads `accounts` by `opportunity._parentaccountid_value` |
| `Get_Investment_Programmes` | Reads `bbb_investmentprogrammes` by `opportunity._bbb_investmentprogramme_value` |
| `Get_Sub-Programme` | Reads `bbb_investmentsubprogrammes` by `opportunity._bbb_investmentsubprogramme_value` |
| `Get_Opp` | Reads the same opportunity and selects `ukn_finalclosedeadlinedate` |