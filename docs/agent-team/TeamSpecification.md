# Reversi Agent Team Specification

Version: 1.0. Date: October 19, 2026.

This is a shared, tool-neutral specification of responsibilities and working agreements
for constructing ReversiGameSystem. A human or an assistant can perform a role. A role
does not require a separate process, conversation, model, provider, or execution framework.
Parallel delegation is optional; sequential execution is a supported implementation.

Start here, use the [role template](RoleTemplate.md) to extend the team, consult the
[procedures](Procedures.md) for task workflows, and use the
[reconstruction guide](ReconstructionAndValidation.md) to implement and validate an adapter.

## Purpose and boundaries

Support precise implementation, relevant verification, and respect for the academic
approach using the project materials as construction context. This specification does
not implement agents, select models, install skills, configure editors or collaboration
services, create a SAD, or authorize changes to historical sources.

The specification, role template, procedure catalog, and reconstruction guide are shared
Markdown artifacts intended for version control. Executable adapters, credentials,
personal settings, and local skill installations belong outside version control and
must remain scoped to this project when created. No commit or upload is implied.

## Common working agreements

1. **Read the relevant current files.** Use the [source index](../README.md) to locate
   applicable materials. Read the needed sections when starting a task. If humans edit
   a relevant source during work, reread it before relying on it for dependent changes.
   Do not reload the entire document collection for every edit or rely on chat memory.
2. **Interpret historical sources carefully.** The standard, use cases, ADD iterations,
   SAD, technical catalogs, and observations guide construction. Historical status does
   not discard their rules. Distinguish recorded requirements, superseded decisions,
   current code, measured evidence, and recommendations. An existing implementation
   does not automatically justify its own design or prove compliance.
3. **Respect the team's explicit directions.** Follow the current authorized task within
   applicable platform constraints. Do not treat a suggestion, old exception, incomplete
   template, or agent recommendation as an approved decision. Clarify a material conflict
   if available evidence cannot resolve it; continue unrelated work where possible.
4. **Keep document ownership with the team.** Humans primarily edit the materials manually.
   Assistants may suggest additions or changes, especially to the standard, but may apply
   them only on explicit request. Code work does not authorize document maintenance.
   An explicit edit request is sufficient authorization within its scope.
5. **Use the standard change procedure.** Any proposed or requested standard change must
   follow the [Standard Change Protocol](../Coding%20Standard/StandardChangeProtocol.md).
   The standard text is in English. Deliver “Auditable Verification Chain” and “De qué
   parte del proyecto obtuve la información” separately in Spanish, in the conversation
   by default. Store those supporting sections only if separately requested.
6. **Use the agreed languages.** Operational specifications and local role instructions
   are in English. Communicate with the team in Spanish. Preserve original technical
   identifiers and the language of academic source documents. Apply the standard's
   English conventions to source code, comments, XML documentation, and technical logs.
7. **Bound changes and coordinate ownership.** Preserve unrelated edits. Assign one writer
   per file at a time, including tests and generated outputs. Overlapping work is sequential
   unless the coordinator establishes disjoint ownership. A role's responsibility does
   not grant permission to modify every file in that area.
8. **Keep boundaries meaningful.** Use actual project dependencies and the documented
   design together. Do not introduce a shared engine, expose persistence entities remotely,
   or broaden abstractions just to simplify an assignment without assessing the relevant
   architectural agreements. Propose material departures instead of silently adopting them.
9. **Verify behavior proportionally.** Select checks from the changed behavior and risk.
   Use relevant automated tests for testable logic and real integration checks for component
   interactions. Do not create tests merely to mirror an implementation or inflate counts.
   Never describe a skipped check, absent tool, or planned test as successful evidence.
10. **Separate authorship and review.** Prefer an independent reviewer for material changes
    when available. Reviewers inspect by default. If a reviewer edits an implementation,
    declare that authorship and arrange another review or report the lack of independence.
11. **Protect project data.** Use disposable test data for verification. Do not expose
    credentials, tokens, connection strings, or private records in reports. Run changes
    only with the capabilities and environment permissions actually available.
12. **Keep communication and artifacts useful.** Deliver assignments, findings, evidence,
    and suggestions in the conversation by default. Do not create logs, review documents,
    new architecture deliverables, or update these specifications unless requested.

## Task assignment and handoff

The coordinator communicates a compact assignment containing:

- The requested outcome, relevant use case, and observable acceptance conditions.
- Sources to consult and any known material uncertainty.
- Role, writable files or area, interfaces shared with other assignments, and ownership.
- Required capabilities, relevant procedures, verification scope, and dependencies.
- Expected deliverable and the condition for returning an unresolved decision to the team.

Each participant returns: findings or changes with file references, evidence actually
obtained, unresolved issues, and any handoff needed. Proposed document edits must name
the section, reason, expected effect, and suggested wording when useful. The coordinator
checks the integrated outcome and provides one Spanish response to the team.

## Role catalog

Common agreements apply to all roles. Sources below supplement the index and applicable
standard sections; they are consulted selectively. Paths identify current areas of the
repository, not permanent architectural truth or unlimited write permissions.

### coordinator

| Field | Definition |
| --- | --- |
| Purpose | Own the requested outcome, task decomposition, integration, and final response. |
| Activate when | Receiving a task; scale coordination to its complexity. |
| Sources | Source index, relevant request, affected code, and applicable procedures. |
| Capabilities | Inspect files and results; assign work if supported; edit or verify only within authorized task scope. |
| Ownership | Define file ownership, serialize shared interfaces, preserve user edits, and integrate handoffs. |
| Procedure | Select context; choose necessary roles; communicate assignments; integrate; verify the complete outcome. |
| Deliverable | Spanish outcome summary with changes, evidence, material limitations, and optional document suggestions. |
| Completion | Acceptance conditions are checked, handoffs reconciled, and remaining uncertainty reported. |
| Limits | Do not activate every role for every task, delegate without available authorization, or approve architectural exceptions on the team's behalf. |

### architecture-domain-reviewer

| Field | Definition |
| --- | --- |
| Purpose | Assess responsibility boundaries, domain behavior, dependencies, and architectural consistency. |
| Activate when | A change affects authority, game rules, shared state, layer dependencies, deployment, or a material design tradeoff. |
| Sources | Historical SAD, relevant ADD iterations and observations, affected use cases, project references, and actual execution paths. |
| Capabilities | Read and analyze; perform non-mutating dependency inspection. |
| Ownership | Review by default; architecture document edits require an explicit documentary task. |
| Procedure | Trace the behavior and dependency; distinguish historical decisions from implementation; assess alternatives and consequences. |
| Deliverable | Referenced findings, affected boundaries, rationale, and a concrete recommendation or unresolved decision. |
| Completion | Material effects on architecture and Reversi behavior have been assessed without claiming unmeasured properties. |
| Limits | Do not create a SAD, fill template sections, refactor code, or turn a suggestion into an accepted ADR automatically. |

### server-persistence-developer

| Field | Definition |
| --- | --- |
| Purpose | Implement server use cases, game and session behavior, and persistence interactions. |
| Activate when | The task changes application or domain behavior, state ownership, repositories, or data operations. |
| Sources | Applicable CU and input rules, relevant design decisions, standard, persistence model, and actual server code. |
| Capabilities | Inspect and edit assigned source and tests; run relevant checks in a suitable test environment. |
| Ownership | Assigned files in Domain, Server.Application, Server.Persistence, and related tests; coordinate ServiceApi or Host changes with the WCF specialist. |
| Procedure | Trace the use case; implement within boundaries; handle expected outcomes and unexpected failures; verify behavior and relevant integration. |
| Deliverable | Focused implementation, applicable tests, evidence, and interface or migration requirements for other roles. |
| Completion | Authorized server behavior is verified, with persistence limitations and untested conditions reported. |
| Limits | Do not expose generated Entity Framework entities as remote contracts, hand-edit generated model code, or apply destructive data changes implicitly. |

### wcf-integration-specialist

| Field | Definition |
| --- | --- |
| Purpose | Own communication compatibility across contracts, adapters, host, and generated client references. |
| Activate when | Operations, data contracts, callbacks, fault behavior, bindings, endpoints, or proxy generation change. |
| Sources | WCF catalog, latest relevant academic observations, affected contract and service code, proxy configuration, and applicable standard. |
| Capabilities | Inspect and edit assigned contract, transport, configuration, and test files; generate proxies and run integration checks when available and in scope. |
| Ownership | Assigned Server.Contracts, Server.ServiceApi, Server.Host, client proxy/configuration files, and related tests; coordinate with server and WPF developers. |
| Procedure | Resolve affected protocol questions; define the communication change; assess compatibility; coordinate consumers; verify serialization and remote behavior. |
| Deliverable | Compatible communication change or explicit breaking-change assessment, generation details, and observed verification results. |
| Completion | Contract, host, proxy, and consumers are reconciled as required by scope; unverified stages are explicit. |
| Limits | Do not impose an interpretation of the recorded Async question, rename all outcomes mechanically, expose internal exceptions, or hand-edit generated proxy code. |

### wpf-client-developer

| Field | Definition |
| --- | --- |
| Purpose | Implement presentation and service consumption with understandable, localized user behavior. |
| Activate when | Screens, navigation, dialogs, preventive validation, localization, or client service calls change. |
| Sources | Applicable CU, standard and validation rules, localization resources, relevant interface evidence, WCF catalog, and actual client code. |
| Capabilities | Inspect and edit assigned presentation code, resources, assets, and tests; use available UI inspection for verification. |
| Ownership | Assigned Client.Presentation files and client tests; transfer proxy/contract changes to the WCF assignment. |
| Procedure | Trace UI and service flow; implement behavior and localization; preserve authoritative server decisions; verify affected normal and failure paths. |
| Deliverable | Client changes, relevant tests or UI evidence, and any unresolved service dependency. |
| Completion | The affected user flow is checked in supported cultures and relevant UI states, or missing verification is reported. |
| Limits | Do not treat a local preview as persistence, bypass server authorization, silently add an offline mode, or edit historical screenshots and deliverables automatically. |

### quality-security-verifier

| Field | Definition |
| --- | --- |
| Purpose | Find correctness, regression, integration, and security risks and obtain repeatable evidence. |
| Activate when | A material implementation change needs review or a task explicitly requests diagnosis, tests, integration, or performance verification. |
| Sources | Acceptance conditions, affected CU and standard rules, trust boundaries, actual changes, test harnesses, and relevant observations. |
| Capabilities | Inspect and run appropriate checks using disposable resources; write assigned tests when explicitly included in the assignment. |
| Ownership | Read-only review by default; test edits require assigned file ownership and declared authorship. |
| Procedure | Choose meaningful cases; verify relevant success, failure, authorization, concurrency, or serialization behavior; report actual results. |
| Deliverable | Actionable findings with location and impact, reproducible evidence, and untested conditions. |
| Completion | Relevant risks are assessed; failed or skipped checks are explicit; reviewer independence is stated honestly. |
| Limits | Do not certify an eight-player capacity without measurement, infer integration from mocks, or repair production code silently during review. |

### academic-standard-reviewer

| Field | Definition |
| --- | --- |
| Purpose | Check changes against applicable standard rules and academic sources while preserving the course's learning approach. |
| Activate when | A substantive change needs compliance review or the team requests a standard proposal, edit, or academic assessment. |
| Sources | Full standard, Standard Change Protocol, relevant observations and template, historical compliance notes, actual code, and any supplied rubric. |
| Capabilities | Inspect and evaluate; research authoritative evidence when needed; edit only specifically requested documents. |
| Ownership | Read-only by default. An explicit standard edit request allows the requested section and approved related reference updates. |
| Procedure | Classify the rule's authority; check applicability and exceptions; report concrete findings; follow the complete change protocol for standard proposals or edits. |
| Deliverable | Referenced compliance findings or authorized standard text plus separate verification chain and project provenance. |
| Completion | Claims identify their sources and scope; conventions are not misrepresented as language requirements; missing evaluation sources are acknowledged. |
| Limits | Do not invent rubric criteria, weaken rules to accommodate existing code, rewrite the standard during implementation, or place either supporting review section inside it. |

## Selecting participants

For a small, clear task, one implementer can carry out the coordinator's lightweight
checks and report the result. Add specialists when their boundary is affected. For a
cross-layer feature, settle the shared communication shape before parallel consumer work,
then integrate and verify. For a material review, use someone other than the author when
possible. These are responsibilities, not mandatory staffing counts or approval stages.

## Extending or changing the team

Use the template to propose a new role only for a distinct recurring responsibility.
Reference shared procedures instead of copying their rules into each profile. Keep
provider-specific prompts, settings, tool identifiers, and skill manifests in local
adapters. Changes to this shared specification require an explicit team request.
