# Neutral Role Template

Use this template to propose or reconstruct a role under the
[team specification](TeamSpecification.md). It is a specification template, not an
executable agent definition. Replace the descriptive placeholders when a role is requested.

## Identity

| Field | Value to define |
| --- | --- |
| Stable identifier | An English identifier describing the responsibility, independent of a provider or model. |
| Display name | A short name the team understands. |
| Purpose | One concrete responsibility and the outcome it protects. |
| Activation conditions | The tasks or risks that justify participation. |
| Non-activation conditions | Work that can stay with another role or does not justify this specialist. |

## Context and authority

| Field | Value to define |
| --- | --- |
| Source references | Relative links to relevant sources and the sections to consult when applicable. |
| Index consultation | Inspect document tables of contents or indices first to pinpoint precise sections before reading content. |
| Source interpretation | Which sources are historical, superseded, incomplete, or unresolved for this task. |
| Accepted task direction | The team's current task and any applicable explicit decisions. |
| Material ambiguity | What would prevent correct execution and require clarification. |

Use the source index instead of duplicating documents. Distinguish source statements,
observed code, test evidence, and recommendations. The common working agreements apply.

## Capabilities and ownership

| Field | Value to define |
| --- | --- |
| Required capabilities | Operations such as inspect files, edit assigned source, run a build, or inspect a UI. |
| Optional capabilities | Useful operations for which a fallback exists. |
| Default mode | Inspection, implementation, verification, or explicitly requested document editing. |
| Assigned write scope | Concrete files or area owned for the task; no ownership means no writing. |
| Generated artifacts | What can be regenerated and how ownership is coordinated; no manual proxy/model edits by default. |
| Shared interfaces | Other roles that must agree on an interface before dependent changes. |
| Boundaries | What this role must not decide, modify, or claim. |
| Fallback | Sequential work or a manual handoff if capabilities are missing. |

Express permissions as behavior requirements. A local adapter must map them to actual
tool access and environment constraints; Markdown alone does not enforce isolation.

## Workflow

1. State the observable outcome and inspect indices to target relevant current files.
2. Invoke applicable procedures on-demand without loading the entire catalog into context.
3. Confirm task ownership with the coordinator; enforce micro-context boundaries without importing extraneous code; do not request redundant user approval
   for work already explicitly authorized.
4. Execute within scope and preserve unrelated changes.
5. Verify the relevant behavior with capabilities actually available.
6. Return focused deltas, line ranges, evidence, limitations, and required handoffs in Spanish (omit unaltered code).

## Output and completion

| Field | Value to define |
| --- | --- |
| Required deliverable | Focused deltas, line ranges, test evidence, recommendation, or interface handoff (no full code reproductions). |
| Evidence format | File/symbol references, conditions, execution result, and remaining uncertainty. |
| Acceptance conditions | Observable conditions for declaring the assignment complete. |
| Review independence | Whether this participant authored changes and who reviews them. |
| Document suggestions | Section, reason, effect, and proposed wording; apply only if explicitly requested. |

## Procedure and validation references

List the applicable entries from the [procedure catalog](Procedures.md). For any standard
proposal or edit, reference the existing Standard Change Protocol. Keep both supporting
review sections outside the standard and in the conversation unless storage is requested.

Define at least one representative task with expected output and one boundary case in
which the role must abstain, defer, or report an unavailable capability. Use the
[reconstruction checks](ReconstructionAndValidation.md) to verify the complete adaptation.
