# Coding Standard Change Protocol

Status: team working agreement, confirmed on October 19, 2026.

This procedure governs proposed additions and changes to the
[C# Video Game Development Standard](C%23%20Video%20Game%20Development%20Standard.md).
It applies regardless of the assistant, model, provider, editor, or execution environment.
It is a separate working procedure, not a new section of the coding standard.

## Authorization and document ownership

The project documents are historical references used to guide construction. Team members
primarily edit them manually. Assistants may suggest improvements, especially to the
standard, but must apply document changes only when explicitly requested by the team.
A code implementation task does not implicitly authorize changing the standard.

A suggestion must identify the affected section, the concrete need, and the expected
effect. Until accepted, it does not replace an existing rule or establish an exception.
An explicit request to edit a section authorizes that edit within its stated scope;
another confirmation is not required for the same authorized work.

This protocol does not authorize completing pending sections, rewriting historical
deliverables, creating a SAD, or updating documents automatically after code changes.

## Rules for preparing a change

1. **Start from the current standard and the real project.** Read the applicable sections
   and inspect relevant ReversiGameSystem code. A rule must address the language, the
   actual project, or a concrete course need. Generic best-practice claims are insufficient.

2. **Check whether the section is ready to be developed.** If there is insufficient project
   evidence or the course has not reached the topic, recommend leaving it pending.
   Do not invent functionality, infrastructure, or technology to justify a section.

3. **Keep the agreed languages.** Text intended for the standard is in English.
   Explanations, analysis, the verification chain, and project provenance are in Spanish.

4. **Research authoritative sources before writing technical rules.** Prioritize the
   C#/ECMA specification, official Microsoft/.NET documentation, official documentation
   for technologies used by the project, recognized standards, and verifiable company
   engineering guides. Prefer an official source over a generic blog when one exists.
   Read the supporting material; do not rely solely on a search snippet or an old citation.

5. **Classify the origin of each rule.** Distinguish language requirements, .NET behavior,
   official recommendations, analyzer or tooling rules, project conventions, rules derived
   from existing code, and technology-specific guidance. Never describe a project choice
   as a C# requirement. A three-parameter limit, for example, is a project convention.

6. **Begin with a simple main rule.** Explain what to do and why before exceptions and
   implementation details. Keep the explanation understandable to a team learning C#.

7. **Use “For this project” for specific working rules.** State what the team must or
   should do in terms that can be checked during code review.

8. **Use normative words deliberately.** Use `must` for mandatory requirements and
   `should` for recommendations with reasonable exceptions. Avoid unnecessary absolutes.

9. **Preserve consistency and established decisions.** Compare the proposed rule with
   naming, null handling, early returns, access modifiers, lambdas, line length, explicit
   types, and other applicable sections. Add cross-references where useful. Do not reopen
   settled conventions without a real contradiction or an explicit team request.

10. **Use small, relevant examples.** Prefer examples from videogames or Reversi, using
    real project code or minimal adaptations where appropriate. Demonstrate one main idea
    and respect the standard's “Code Example Scope”. Correct examples must comply with
    the substantive rules applicable to the behavior shown. Avoid advanced or condensed
    techniques unless they provide a clear benefit.

11. **Include “Correct” and “Incorrect” examples with explanations.** The incorrect
    example must demonstrate the specific violation being discussed without unrelated
    mistakes that obscure it. Explain why each example complies or violates the rule.

12. **Prepare an “Auditable Verification Chain” as separate review material.** For each
    important rule, identify its supporting evidence and classification. This chain must
    remain outside the standard, including its numbered sections, appendices, and references.

13. **Maintain APA 7 references carefully.** Reuse existing entries and avoid duplicates.
    Add sources only when needed. Identify new reference entries and update affected
    citations if author/year suffixes such as `2021a` and `2021b` change. Use canonical
    URLs without tracking parameters. Citations and bibliographic entries that support
    the normative text may remain in the standard; they are not the verification chain.

14. **Provide project provenance separately.** Use the Spanish heading “De qué parte del
    proyecto obtuve la información” and identify relevant files, classes, methods, and
    fragments. This section must remain outside the standard, like the verification chain.

15. **Match the existing depth and format.** Prepare the main rule, rationale, specific
    project rules, examples, explanations, and necessary references at a level consistent
    with the developed sections. Supply the verification chain and provenance separately.

16. **Never silently change approved content.** If a proposed section requires changing
    another section, state exactly what text would change and why. If that expands the
    requested scope, propose the additional edit rather than applying it automatically.

## Required separation of deliverables

### A. Text for the standard

Provide the English section or targeted edit, including its rule, rationale, project
rules, cross-references, examples, explanations, and necessary citations and bibliography
changes. Identify the intended section and avoid unrelated rewrites.

Do not insert the following two review sections anywhere in the standard.

### B. Auditable Verification Chain

Provide this outside the standard, with explanations in Spanish. A suitable table is:

| Regla | Evidencia verificable | Clasificación |
| --- | --- | --- |
| Regla propuesta | Fuente y ubicación que respaldan la afirmación | Lenguaje, .NET, recomendación oficial, herramienta, convención o código existente |

Identify project conventions honestly; an external source need not mandate a convention
for the team to choose it. Do not invent evidence or claim a source supports more than it does.

### C. De qué parte del proyecto obtuve la información

Provide this outside the standard and in Spanish. Explain which project files, classes,
methods, and behavior informed the change, and distinguish existing implementation from
proposals. Use concrete references without copying unnecessary code or sensitive data.

These two supporting sections can be delivered in the conversation. Their production
does not authorize creating or updating additional repository files. Store them in a
separate review document only if the team requests it.

## Completion criteria for an authorized edit

- The change stays within the requested scope and preserves unrelated content.
- Its language, format, examples, cross-references, and source classifications follow
  this procedure and the applicable existing standard.
- Relevant technical claims have been checked against supporting sources and project code.
- Citations and reference entries are consistent; local links affected by the edit work.
- Both supporting review sections are delivered separately from the standard.
- Any unresolved contradiction or suggested out-of-scope change is reported explicitly.
- The assistant reports which section changed and how it checked the result, without
  claiming code compliance or successful tests that were not actually verified.

## Origin

The procedure consolidates the protocol previously summarized with the team in the chat
“Estructura Estándar Auditable” and the working agreements confirmed in the current chat.
The team's correction explicitly places both supporting sections outside the standard.
Chat history is provenance for this procedure; it does not replace technical sources for
individual coding rules.
