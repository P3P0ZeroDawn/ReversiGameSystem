# C# Coding Standard Compliance

## Scope

This record covers the handwritten C# source in `src` and `tests`. Entity Framework
files marked as auto-generated are excluded because manual changes would be overwritten
when the model is regenerated.

## Enforced rules

- Source identifiers, comments, and XML documentation use English.
- Private instance fields use `_camelCase`.
- Explicit types are preferred over `var`.
- Public APIs include XML documentation.
- Handwritten lines are limited to 100 characters.
- Expected failures use specific exception types or result contracts.
- Tests follow the Arrange, Act, Assert structure.
- Cyclomatic complexity above 10 is treated as a refactoring warning.

The repository-level `.editorconfig` and build properties enforce the rules that the
current compiler and analyzers can verify automatically. The remaining rules are checked
during code review.

## Reviewed exceptions

The three-parameter guideline is retained as the default. The following existing cases
are accepted because replacing their parameters with artificial wrapper objects would
reduce clarity without reducing responsibility:

- Immutable commands, DTO-like domain records, and presentation option objects whose
  constructor arguments map one-to-one to their read-only properties.
- WPF view constructors that receive the state required to render one screen.
- Prototype gameplay operations that represent one atomic move or match transition.
- Composition-root factory methods that wire a cohesive set of runtime dependencies.

Methods above 30 lines are refactoring warnings rather than automatic failures. They are
retained only when they are low-complexity UI orchestration, explicit result mapping, or
specific exception-boundary code. Application use cases are split into focused helpers.

## Deferred item

Centralized structured logging and crash reporting are intentionally deferred for the
current local-runtime milestone. This is the only known standard item not implemented by
this correction pass.
