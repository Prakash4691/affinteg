Account table fields:

name - required
ukn_relationshiplead - required (lookup to systemuser table)
address1_line1 - required
address1_postalcode - required

======================================================================================================================

Contact table fields:

lastname - required
firstname - optional
emailaddress1 - required
======================================================================================================================

Lead table fields:

subject - required
bbb_dealname - required
lastname - required ( pickup from contact)
companyname - pickup from account
parentaccountid - required (account table)
parentcontactid - required (contact table)
bbb_investmentprogramme - required (investment programme table)
bbb_investmentsubprogramme - optional (investment subprogramme table) ( required for facility creation via opportunity)
ukn_sector - required (sector optionset)
ukn_initialcontactdate - required
ukn_accountlead - required (lookup to systemuser table)
ukn_expectedbbbcommitment - recommended (mandatory for opportunity and facility)
transactioncurrencyid - required (lookup to transactioncurrency table)

=====================================================================================================================

Opportunity table fields:

name - (picked up from lead)
parentcontactid - (picked up from lead)
parentaccountid - (picked up from lead)
bbb_dealname - (picked up from lead)
transactioncurrencyid - (picked up from lead)
bbb_investmentprogramme - (picked up from lead)
bbb_investmentsubprogramme - (picked up from lead)
ukn_subassetclass - required (lookup to subassetclass table)
ukn_fundid - required (lookup to funding vehicle table)
bbb_bbbcommitment - 
ukn_accountlead - (picked up from lead)
ukn_sector - (picked up from lead)
ukn_topupexistingfacility - required (optionset, set to no default)
ukn_dryclose - (optionset, set to no default)
ukn_completiondate
bbb_totalcommitment
ukn_drycloseexpirydate
ukn_firstexpirydate
ukn_finalclosedeadlinedate
ukn_maximumbbbcommitment
ukn_daysremaining

BPF required fields:
bbb_eoireceived - set as completed
bbb_totalcommitment - number
ukn_targetfundsize - number
ukn_wipdiscussion - set as completed
ukn_funddomicile - optionset
ukn_receivedcompletedquestionnaire - set as yes
ukn_datequestionnairereceived - date

ukn_intromeetingdate - date
ukn_investmentsummarynote - set as completed
ukn_prepitchpaper - set as completed
ukn_pitchmeetingdate - date
ukn_teamdecision - set as completed
ukn_directorapproval - set as completed
ukn_feedbackprovided - set as completed
ukn_bpcukcontentrequirement - number
ukn_bpcsectors - string

ukn_iccommitteedate - date
ukn_onsitemeeting - set as completed
ukn_referencingdone - set as completed
bbb_ddreport - set as completed
ukn_icpaperdates - set as completed
ukn_iccommittee - set as completed
bbb_completionexpected - date
ukn_iccommitteeapproved - date

ukn_termsheetissued - set as completed
ukn_lpaagreed - set as completed
ukn_sideletteragreed - set as completed
ukn_signoff - set as completed
ukn_onboardingprocesscomplete - set as completed
ukn_completiondate - date
ukn_finalclosedeadlinedate - date
ukn_firstexpirydate - date
ukn_bpffinished - set as yes

====================================================================================================================

KycRequest table fields:

ukn_bbbproductmanagerrelationshiplead - required (picked up from opportunity) relationship lead of the opportunity
ukn_programme - required (picked up from opportunity)
ukn_subprogramme - optional (picked up from opportunity)
ukn_deliverypartner - required (picked up from opportunity) account of the opportunity
ukn_originatingopportunity - required (lookup to opportunity table) opportunity record itself
ukn_countryofprincipalplaceofbusiness - required (lookup to country table) default setting it to united kingdom


