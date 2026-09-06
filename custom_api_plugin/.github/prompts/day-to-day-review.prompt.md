---
name: day-to-day-review
description: Review the current code changes or relevant codebase for defects, regressions, maintainability risks, inadequate logging and diagnostic gaps, with additional checks for Microsoft Dataverse plug-ins and custom APIs. Use before committing code, creating a pull request or investigating unexpected behaviour.
---

<!-- Tip: Use /create-prompt in chat to generate content with agent assistance -->

Review the current Git diff and directly affected execution paths.

Use the installed `dataverse-plugin-observability-review` skill for Dataverse-specific checks. Use the installed code-review, debugging and general observability skills where relevant. Avoid repeating their full checklists in the response.

Prioritise:

- correctness and potential regressions;
- null handling and invalid assumptions;
- swallowed or misleading exceptions;
- inadequate diagnostic logging;
- security or sensitive-data exposure;
- missing or weak regression tests.

For Microsoft Dataverse plug-ins and custom APIs, also check:

- correct use of `IPluginExecutionContext`, `ITracingService` and `IOrganizationService`;
- target, input parameter and entity-image validation;
- stage, mode, depth and recursion behaviour;
- early returns and important branches with insufficient tracing;
- service calls and failures without useful diagnostic context;
- whether Plug-in Trace Logs would allow a production failure and its execution path to be diagnosed;
- unsafe logging of secrets, personal data or complete entity payloads;
- correct `InvalidPluginExecutionException` handling;
- alternate-key and `UpsertRequest` create-versus-update behaviour;
- unnecessary retrieves, updates or service calls.

For every material finding, provide:

- severity: Critical, High, Medium or Low;
- file and method;
- evidence and triggering conditions;
- potential impact;
- minimal correction;
- suggested regression test.

Group duplicate findings by root cause.

Do not modify files unless explicitly requested.
Do not report subjective style preferences.
Do not invent plug-in registration details.
Distinguish confirmed defects from items requiring verification.
State which tests or builds were actually run.

If no material issue is found, say so and summarise the reviewed scope.