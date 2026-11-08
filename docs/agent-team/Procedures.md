# Shared Procedure Catalog

This catalog describes workflows by outcomes and capabilities. It is not a skill manifest
and does not require an agent framework. A local skill can implement a procedure by
referencing it rather than maintaining a divergent copy. The
[team agreements](TeamSpecification.md) apply throughout.

## Context selection

**Use when:** starting a task or when a relevant source changes during work.

1. Locate applicable documents using the [source index](../DocumentationIndex.md). If a document contains a table of contents or index, inspect it first to target specific sections or line ranges rather than loading entire files.
2. Read relevant current sections and inspect the actual affected execution path.
3. Identify the use case, standard rules, academic observations, and design decisions
   that influence this task. Historical sources inform construction without proving
   current implementation. Do not treat every earlier statement as equally current.
4. Check any material conflict against explicit team directions and available evidence.
   Surface a decision that cannot be resolved safely from the information available.
5. Return the relevant constraints and uncertainty in a compact micro-context assignment or conversation.

**Output:** focused context with source references. No source edits, new context logs,
or academic deliverables are implied.

## Scoped implementation and integration

**Use when:** the team requests a feature, fix, or refactoring.

1. Define the user-visible or technical outcome and acceptance conditions.
2. Assign the smallest useful set of roles and one writer per file. Preserve user work.
3. Settle affected shared interfaces before parallel dependent changes. Coordinate
   contract, service, generated reference, UI, and tests when the same flow crosses them.
4. Implement the authorized behavior under relevant standard and architecture constraints.
5. Run targeted verification, integrate dependent changes, and check the complete outcome.
6. Return results as focused deltas, line numbers, and evidence; propose documentation changes separately if useful.

**Output:** integrated change and evidence. Updating documentation, creating a SAD,
removing all LocalRuntime consumers, or migrating unrelated flows requires task scope;
none is an automatic consequence of implementing a feature.

## Architecture and Reversi domain review

**Use when:** authority, rules, state, dependencies, concurrency, or deployment changes.

Consult the [historical SAD](../Design%20Project/Documento-Entrega.md), relevant
[ADD history](../DocumentationIndex.md), [observations](../Academic%20Observations/Observaciones.md),
affected use cases, and actual dependencies. Trace who makes the authoritative decision,
owns active state, persists results, and provides UI feedback. Assess compatibility with
documented client/server separation and deliberate rule-validation duplication where
applicable. Do not recreate a shared engine simply to remove duplication mechanically.

Distinguish the eight-player operating assumption from demonstrated capacity and the
online delivery priority from an optional offline extension. For material departures,
explain alternatives, consequences, and the decision needed from the team.

**Output:** referenced findings and recommendations. No ADR, SAD, or iteration edits
are authorized by this review.

## Server and persistence implementation

**Use when:** application behavior, sessions, game state, repositories, or storage changes.

Use the relevant CU in the [first delivery](../Project%20Specs/PRIMERA%20ENTREGA%20PROYECTO.md),
[validation rules](../../ValidationRules.md), domain code, application ports, and actual
persistence implementation. Keep use-case decisions distinct from transport mapping and
database access. Verify authorization and validation on the authoritative side, appropriate
outcomes, ownership of resources, and integrity of the affected operation.

Test isolated logic at the lowest useful level and verify real persistence interaction
where it is part of the acceptance conditions. Use a disposable environment; report
unavailable database access instead of inventing an integration result. Coordinate any
schema or generated model change rather than hand-editing generated entities.

**Output:** assigned changes, relevant evidence, and dependencies for integration.

## WCF boundary change

**Use when:** service or data contracts, faults, callbacks, endpoints, or proxies change.

1. Read the relevant [WCF catalog](../WcfIntegrationContracts.md), academic observations,
   source contracts, adapters, configuration, and consumers.
2. Identify the intended operation semantics, expected results, unexpected-failure
   handling, serialization, callback responsibility, and compatibility effects.
3. If the task depends on the recorded Async or naming questions, resolve the relevant
   interpretation from evidence or request clarification. Do not apply global renaming
   or signature changes merely to eliminate an unresolved note.
4. Coordinate the affected server and client ownership. Regenerate references when
   required and supported instead of editing generated proxy source manually.
5. Verify affected serialization and contract behavior and, when required, exercise the
   real communication boundary. Distinguish contract availability, hosted endpoint,
   generated reference, and client integration; one does not prove the other stages.

**Output:** integrated protocol change, compatibility assessment, and observed evidence.
Catalog updates remain suggestions unless the team explicitly requests them.

## WPF user-flow change

**Use when:** presentation, localization, navigation, validation, or service consumption changes.

Read the affected CU, [validation rules](../../ValidationRules.md), standard, actual client
code, and current localization resources. Historical screenshots are context, not proof
of today's UI. Separate preventive input checks and previews from server decisions and
successful persistence. Keep failures and unavailable data distinct from successful
empty results. Check the affected flow in en-US and es-MX, including relevant cancellation,
service failure, guest restrictions, layout, and pending-operation states.

Use the existing test harness for testable client logic. If UI inspection is unavailable,
report that limitation and provide concrete manual verification steps. Do not claim visual
verification from source inspection alone.

**Output:** affected presentation changes and evidence, with service dependencies stated.

## Quality and security verification

**Use when:** evaluating a material change or an explicit verification request.

1. Read the actual changes, acceptance conditions, and affected trust boundaries.
2. Select relevant normal, failure, boundary, authorization, concurrency, or compatibility
   cases. Avoid duplicating coverage without a concrete risk.
3. Inspect the project's actual build and test entry points. Do not assume a standard test
   command discovers executable test harnesses or that the aggregate solution covers every
   client test. Choose affected projects from current files.
4. Run supported checks with non-production data. Record conditions, actual results, and
   whether a failure is caused by the change or is an environmental or existing limitation.
5. Report reproducible findings with location, impact, and evidence. A clean review is not
   proof of zero defects. Mock-based results do not establish real integration behavior.

**Output:** findings, performed checks, failed/skipped checks, and independence declaration.
No additional repository report file is required unless requested.

## Academic and standard compliance review

**Use when:** assessing adherence to applicable academic or coding requirements.

Consult the [full standard](../Coding%20Standard/CSharp%20Video%20Game%20Development%20Standard.md),
[observations](../Academic%20Observations/Observaciones.md), relevant supplied materials,
and actual code. For each material finding, identify the rule and section, applicability,
observed evidence, and impact. Distinguish a language requirement from a project convention,
a refactoring warning from an automatic failure, and an old exception from confirmed scope.

Respect the template's distinction between partial and final deliveries; incomplete final
sections in the historical partial SAD are not automatically defects. Missing Manual or
Evaluation materials limit what can be certified but do not block unrelated construction.

**Output:** concrete findings and suggestions in Spanish. A compliance review does not
authorize rewriting the standard or its exceptions. The full supporting package below is
required when proposing actual standard text, rather than merely citing an existing rule.

## Standard proposal or explicitly requested edit

**Use when:** proposing a specific addition/change or executing an explicit standard edit.

Follow the [Standard Change Protocol](../Coding%20Standard/StandardChangeProtocol.md)
directly; it is the shared source for the full procedure. Research authoritative evidence,
check actual code, preserve approved decisions and scope, and prepare the following:

| Deliverable | Language | Destination |
| --- | --- | --- |
| Standard section or targeted edit, with applicable citations and references | English | Proposed in conversation, or applied to the standard if explicitly requested |
| Auditable Verification Chain | Spanish explanations | Conversation, outside the standard |
| De qué parte del proyecto obtuve la información | Spanish | Conversation, outside the standard |

Neither supporting section belongs inside the standard, including its appendices. Create
a separate supporting Markdown only when the team requests storage. A suggestion does
not authorize applying its text or expanding to adjacent sections.

## Adding a procedure or local skill

Propose a distinct procedure only when recurring work needs it. Define its trigger,
required context, capabilities, steps, output, completion evidence, and boundary case.
An explicit request is needed to modify this shared catalog. A local skill adapter must
reference the approved procedure, describe when it applies, and preserve its permissions
and output boundaries. New capabilities or skill installation do not expand authorization.
