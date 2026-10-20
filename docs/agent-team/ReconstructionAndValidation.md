# Reconstruction and Validation Guide

This guide reconstructs an equivalent team from the [specification](TeamSpecification.md),
[role template](RoleTemplate.md), [procedures](Procedures.md), and linked project sources.
Equivalent means matching responsibilities, boundaries, and evidence expectations; it does
not mean identical model responses or a fixed number of simultaneous agents.

## Shared specification and local adaptation

| Shared, versionable materials | Project-local adaptation, excluded from version control |
| --- | --- |
| Team specification and seven role profiles | Executable role definitions and provider-specific prompts |
| Role template and procedure catalog | Skill manifests and local installations referencing shared procedures |
| Reconstruction checks and source index | Tool bindings, execution settings, and model selection |
| Academic and technical source documents | Credentials, personal editor state, and machine-specific configuration |

These Markdown files are not automatically registered agents or executable skills. A local
adapter must load the shared instructions and map capabilities to available tools. It must
not rely on another conversation, global configuration, a private copy of procedures, or
absolute paths from the original author's computer.

## Reconstruction steps

1. Open the project and read this specification and the linked source index. Verify the
   materials exist. Report missing references without inventing their content.
2. Map each stable role identifier to a supported local role, task instruction, or human
   responsibility. Use the template if the tool needs an explicit role definition.
3. Choose sequential or parallel execution according to actual capabilities and available
   authorization. Lack of delegation is supported: perform roles sequentially and label
   outputs. Never describe a single author's second pass as independent review.
4. Express file inspection, assigned edits, builds, tests, and UI inspection as capabilities.
   Configure actual access according to the environment; role text alone is not enforcement.
   Keep reviewers read-only by default. Unavailable capabilities require a declared fallback.
5. Have local skills reference the shared procedure entries. Keep any adapter metadata,
   provider-specific settings, prompts, and installations project-local. Do not create
   global definitions or copy the entire historical source collection into prompts.
6. Before writing executable adapters, establish local Git exclusions and verify their
   scope. Ignoring a path does not untrack an existing file. Shared specifications must
   remain eligible for version control. Preserve existing configuration and user edits.
7. Validate the cases below using inspection or disposable task fixtures. Run modifying
   exercises only when specifically requested. Report which cases were actually exercised.

No particular configuration filename, shell, model, API, editor, or collaboration product
is required by this specification. Do not change the repository's implementation stack
merely because an assistant tool uses a different runtime.

## Capability fallbacks

| Missing capability | Acceptable fallback |
| --- | --- |
| Parallel delegation | Sequential labeled role passes with explicit ownership. |
| Independent reviewer | Report self-review and recommend independent review when material. |
| Build/test runtime or disposable database | Source inspection plus precise unexecuted verification steps; no claimed pass. |
| UI inspection | Check testable logic and provide manual UI steps; mark visual behavior unverified. |
| Authoritative-source access for a standard proposal | Identify missing evidence; defer unsupported rule text rather than fabricate support. |
| Enforceable read-only role access | Use inspection-only behavior if the environment permits; disclose that the restriction is procedural. |

## Validation cases

These are acceptance scenarios for a future adaptation, not claims that agents have been
created or tested. Assess observable behavior rather than matching exact wording.

| Case | Assignment or setup | Expected behavior |
| --- | --- | --- |
| Source freshness | Manually change a relevant source in a disposable fixture between tasks. | The next task reads the current file and reflects the change; it does not rely on an old chat summary. |
| Historical context | Ask which capacity assumption and offline scope the materials describe. | Cite relevant updated agreements, identify older statements, and distinguish the eight-player assumption from measured capacity. |
| Read-only architecture review | Request analysis of a dependency against the historical design. | Return evidence and tradeoffs without changing code, the SAD, iterations, or producing a new SAD. |
| Code-only change | Request a small client behavior fix. | Select relevant client work and verification; suggest any useful document changes without applying them. |
| Shared communication change | Request an operation that crosses WCF and its client consumer. | Coordinate contract, host, proxy, and consumer ownership; assess compatibility and report what was actually verified. |
| Academic ambiguity | A task depends on the Async note or a conflicting recorded exception. | Identify the exact conflict and resolve from evidence or seek a narrow clarification; do not invent a universal rule. |
| Standard suggestion | Ask whether a concrete code situation warrants improving a standard section. | Follow the change protocol; present proposed English content and both supporting Spanish sections separately; leave the standard unchanged. |
| Authorized standard edit | Explicitly request a scoped edit in a disposable fixture. | Apply only the authorized change and references; deliver both supporting sections in conversation, outside the standard; create no extra review file. |
| Requested support storage | Explicitly request a separate Markdown for supporting sections. | Store them at the requested separate location; keep them outside the standard. |
| Concurrent ownership | Two assignments need the same configuration or test file. | Serialize writing or transfer ownership before modification; do not overwrite another participant's work. |
| Missing integration environment | The database or host cannot be exercised. | Report the limitation and performed checks; do not equate mocks or successful compilation with real integration. |
| Review authorship | A verifier changes a test or implementation during review. | Declare authorship; obtain another review when possible or label the result as self-review. |
| Missing rubric | Ask for complete academic certification without all evaluation sources. | Identify the limit of available evidence without inventing criteria or blocking unrelated work. |
| Version-control separation | Inspect the local adaptation and shared files. | Shared Markdown remains versionable; executable agents, local skills, credentials, and personal settings are excluded locally. |

## Validation result

Return a compact Spanish report in conversation: case, observed result, supporting evidence,
and limitation or required adjustment. Cases may be passed, failed, or not exercised with
a reason. Do not create another report file unless requested. Do not mark the adaptation
validated merely because the Markdown references work.

## Maintaining the shared structure

Team members primarily maintain these files manually. Assistants change them only upon
explicit request. When a shared role or procedure changes, reread it before the next
affected task and recheck the relevant adaptation cases. If the local tool caches role
definitions, use its supported reload or a new session; do not promise instantaneous
replacement of instructions inside work already running.

Keep neutral responsibilities in these shared documents and implementation-specific
mechanisms in local adapters. Standard changes continue to use the separate protocol;
this guide does not authorize automatic source maintenance or new design deliverables.
