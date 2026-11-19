## Table of Contents

- [1. Introduction](#1.%20Introduction)
    - [1.1 Problem Statement](#1.1%20Problem%20Statement)
    - [1.2 Purpose](#1.2%20Purpose)
- [2. Language](#2.%20Language)
    - [2.1 Source Code Language](#2.1%20Source%20Code%20Language)
    - [2.2 User-Facing Language and Localization](#2.2%20User-Facing%20Language%20and%20Localization)
- [3. Naming Conventions](#3.%20Naming%20Conventions)
    - [3.1 General Identifier Rules](#3.1%20General%20Identifier%20Rules)
    - [3.2 Namespaces](#3.2%20Namespaces)
    - [3.3 Classes](#3.3%20Classes)
    - [3.4 Interfaces](#3.4%20Interfaces)
    - [3.5 Enumerations](#3.5%20Enumerations)
    - [3.6 Enumeration Members](#3.6%20Enumeration%20Members)
    - [3.7 Fields](#3.7%20Fields)
    - [3.8 Properties](#3.8%20Properties)
    - [3.9 Local Variables](#3.9%20Local%20Variables)
    - [3.10 Parameters](#3.10%20Parameters)
    - [3.11 Constants](#3.11%20Constants)
    - [3.12 Methods](#3.12%20Methods)
    - [3.13 Events](#3.13%20Events)
    - [3.14 Test Classes](#3.14%20Test%20Classes)
    - [3.15 Test Methods](#3.15%20Test%20Methods)
    - [3.16 Prefixes and Suffixes](#3.16%20Prefixes%20and%20Suffixes)
    - [3.17 WPF Controls, Resource Keys, and Client Asset Files](#3.17%20WPF%20Controls%2C%20Resource%20Keys%2C%20and%20Client%20Asset%20Files)
- [4. Code Style and Formatting](#4.%20Code%20Style%20and%20Formatting)
    - [4.1 General Formatting](#4.1%20General%20Formatting)
    - [4.2 Indentation](#4.2%20Indentation)
    - [4.3 Braces](#4.3%20Braces)
    - [4.4 Line Length](#4.4%20Line%20Length)
    - [4.5 Line Wrapping](#4.5%20Line%20Wrapping)
    - [4.6 Horizontal Whitespace](#4.6%20Horizontal%20Whitespace)
    - [4.7 Vertical Whitespace](#4.7%20Vertical%20Whitespace)
    - [4.8 Using Directives](#4.8%20Using%20Directives)
    - [4.9 Namespace Declarations](#4.9%20Namespace%20Declarations)
    - [4.10 Early Returns and Final Assignment](#4.10%20Early%20Returns%20and%20Final%20Assignment)
- [5. C# Programming Practices](#5.%20C%23%20Programming%20Practices)
    - [5.1 Access Modifiers](#5.1%20Access%20Modifiers)
    - [5.2 Auto-Implemented Properties and Property Accessors](#5.2%20Auto-Implemented%20Properties%20and%20Property%20Accessors)
    - [5.3 Immutability and readonly Usage](#5.3%20Immutability%20and%20readonly%20Usage)
    - [5.4 Constants and Magic Values](#5.4%20Constants%20and%20Magic%20Values)
    - [5.5 Method Responsibility](#5.5%20Method%20Responsibility)
    - [5.6 Don't Repeat Yourself (DRY)](#5.6%20Don%27t%20Repeat%20Yourself%20%28DRY%29)
    - [5.7 Method Size](#5.7%20Method%20Size)
    - [5.8 Parameters](#5.8%20Parameters)
    - [5.9 Cyclomatic Complexity](#5.9%20Cyclomatic%20Complexity)
    - [5.10 Nullable Reference Types](#5.10%20Nullable%20Reference%20Types)
    - [5.11 Null Handling](#5.11%20Null%20Handling)
    - [5.12 Pattern Matching](#5.12%20Pattern%20Matching)
    - [5.13 LINQ Usage](#5.13%20LINQ%20Usage)
    - [5.14 Lambda Expressions](#5.14%20Lambda%20Expressions)
    - [5.15 Events and Delegates](#5.15%20Events%20and%20Delegates)
    - [5.16 Resource Disposal](#5.16%20Resource%20Disposal)
    - [5.17 Serialization](#5.17%20Serialization)
    - [5.18 Explicit Local Variable Types](#5.18%20Explicit%20Local%20Variable%20Types)
    - [5.19 Conditional Operator](#5.19%20Conditional%20Operator)
    - [5.20 Thread Synchronization and Concurrency](#5.20%20Thread%20Synchronization%20and%20Concurrency)
    - [5.21 Semaphores and Asynchronous Mutual Exclusion](#5.21%20Semaphores%20and%20Asynchronous%20Mutual%20Exclusion)
    - [5.22 Atomic Operations](#5.22%20Atomic%20Operations)
    - [5.23 Concurrent Collections](#5.23%20Concurrent%20Collections)
    - [5.24 You Aren't Gonna Need It (YAGNI)](#5.24%20You%20Aren%27t%20Gonna%20Need%20It%20%28YAGNI%29)
    - [5.25 Local Functions](#5.25%20Local%20Functions)
    - [5.26 Asynchronous Operations](#5.26%20Asynchronous%20Operations)
- [6. Error Handling, Logging, and Diagnostics](#6.%20Error%20Handling%2C%20Logging%2C%20and%20Diagnostics)
    - [6.1 Exception Handling](#6.1%20Exception%20Handling)
    - [6.2 Custom Exceptions](#6.2%20Custom%20Exceptions)
    - [6.3 Logging](#6.3%20Logging)
    - [6.4 Log Levels](#6.4%20Log%20Levels)
    - [6.5 Log Message Conventions](#6.5%20Log%20Message%20Conventions)
    - [6.6 Player-Facing Error Messages](#6.6%20Player-Facing%20Error%20Messages)
    - [6.7 Unhandled Exceptions and Diagnostic Reporting](#6.7%20Unhandled%20Exceptions%20and%20Diagnostic%20Reporting)
- [7. Comments and Documentation](#7.%20Comments%20and%20Documentation)
    - [7.1 General Comment Rules](#7.1%20General%20Comment%20Rules)
    - [7.2 Implementation Comments](#7.2%20Implementation%20Comments)
    - [7.3 XML Documentation Comments](#7.3%20XML%20Documentation%20Comments)
    - [7.4 Public API Documentation](#7.4%20Public%20API%20Documentation)
    - [7.5 TODO Comments](#7.5%20TODO%20Comments)
- [8. Input Validation and Security](#8.%20Input%20Validation%20and%20Security)
    - [8.1 Trust Boundaries](#8.1%20Trust%20Boundaries)
    - [8.2 Input Validation](#8.2%20Input%20Validation)
    - [8.3 File System Access](#8.3%20File%20System%20Access)
    - [8.4 Credential and Secret Management](#8.4%20Credential%20and%20Secret%20Management)
- [9. Testing and Quality Assurance](#9.%20Testing%20and%20Quality%20Assurance)
    - [9.1 Testing Strategy](#9.1%20Testing%20Strategy)
    - [9.2 Unit Testing](#9.2%20Unit%20Testing)
    - [9.3 Integration Testing](#9.3%20Integration%20Testing)
    - [9.4 Atomic Test Structure](#9.4%20Atomic%20Test%20Structure)
- [10. Version Control and Collaboration](#10.%20Version%20Control%20and%20Collaboration)
    - [10.1 Commit Messages and Atomicity](#10.1%20Commit%20Messages%20and%20Atomicity)
    - [10.2 Repository Hygiene and Sensitive Data](#10.2%20Repository%20Hygiene%20and%20Sensitive%20Data)
- [11. References](#11.%20References)
- [12. AI Use Declaration](#12.%20AI%20Use%20Declaration)

## 1. Introduction

This development standard defines the coding rules and practices governing C# video game software development, including desktop presentation (WPF), remote communication contracts (WCF), application logic, persistence (Entity Framework), and automated testing. It prioritizes readability, consistency, maintainability, and defect prevention over condensed syntax (Microsoft, 2025f).

### 1.1 Problem Statement

C# permits expressing identical behavior in multiple syntactic forms that are accepted by the compiler without enforcing consistent naming, formatting, null safety, error handling, or test organization (Microsoft, 2026b). Without verifiable rules, stylistic divergence increases cognitive load, hides defects, and complicates code reviews.

### 1.2 Purpose

This standard establishes clear, verifiable, and consistently applicable rules to:

- Ensure uniform source-code readability and structure across contributors.
- Reduce accidental complexity, duplicated logic, and ambiguous `null` contracts.
- Standardize error handling, structured logging, security boundaries, and automated testing.
- Enable automated enforcement through compiler diagnostics, code analyzers, and `.editorconfig`.

This standard complements the C# language specification and official .NET guidance without replacing them.

**Code Example Scope:** Each example demonstrates only its section concept and omits unrelated surrounding declarations. Examples contain no code comments and use APIs compatible with the supported target framework. Correct examples must satisfy the other applicable rules; incorrect examples should isolate the stated violation. Documentation examples show text or XML content without C# comment markers.

Language requirements are distinct from the naming and review conventions selected here. A construct not mentioned in this standard requires review when it introduces unfamiliar complexity, but its omission alone does not make it invalid C#. Keep feature architecture and business rules in their own design documents.

---

## 2. Language

### 2.1 Source Code Language

Write handwritten identifiers, comments, documentation, and technical logs in English; generated code retains its original language and structure (Microsoft, 2026b).

#### Correct

```csharp
int remainingLives = player.RemainingLives;
```

The identifier describes the value in English.

#### Incorrect

```csharp
int vidasRestantes = player.RemainingLives;
```

The handwritten identifier does not follow the agreed language.

### 2.2 User-Facing Language and Localization

Store culture-dependent player-facing text in localization resources rather than UI markup or C# literals. All configured cultures must share stable English keys and compatible format placeholders; localize images with embedded text without duplicating language-neutral assets (Microsoft, n.d.-i).

#### Correct

```csharp
string turnBanner = string.Format(
    LocalizationManager.Instance["Combat_CurrentTurnFormat"],
    activePlayerName);
```

The displayed text comes from the resource dictionary for the active culture.

#### Incorrect

```csharp
string turnBanner = "Current turn: " + activePlayerName;
```

The literal cannot follow the selected user interface culture.

---

## 3. Naming Conventions

### 3.1 General Identifier Rules

Identifiers must use descriptive domain names that communicate intent without inspecting the implementation. Unclear abbreviations and consecutive underscores (`__`, reserved for compiler-generated names) are prohibited. Single-letter names are permitted only for trivial local loop counters (Microsoft, 2026b).

#### Correct

```csharp
int remainingLives = player.RemainingLives;
```

The name states the stored meaning without an unexplained abbreviation.

#### Incorrect

```csharp
int rl__ = player.RemainingLives;
```

The abbreviation hides intent and uses a reserved double underscore pattern.

### 3.2 Namespaces

Namespace names must use **PascalCase** separated by periods to form a clear hierarchy starting with the root product name followed by the architectural layer or functional area, such as `Gameplay`, `Combat`, `Contracts`, or `Persistence` (Microsoft, 2023f).

#### Correct

```csharp
namespace GameProject.Gameplay.Combat;
```

The namespace uses PascalCase and a readable hierarchy.

#### Incorrect

```csharp
namespace game_project.gameplay_combat;
```

The namespace replaces the required hierarchy with lowercase words and underscores.

### 3.3 Classes

Use PascalCase nouns that identify one responsibility (Microsoft, 2025k). Suffixes such as ApplicationService and Repository are examples of responsibility-based names, not mandatory suffixes or architectural requirements. Choose names that describe the actual role without unnecessary technology details, and avoid types that merely forward calls without a distinct responsibility.

#### Correct

```csharp
public sealed class InventoryRepository : IInventoryRepository
{
}
```

The name describes the responsibility without exposing its implementation technology.

#### Incorrect

```csharp
public sealed class EntityFrameworkInventoryRepository : IInventoryRepository
{
}
```

The name unnecessarily couples the repository to a persistence technology.

### 3.4 Interfaces

Interface names must use **PascalCase** prefixed with `I` and describe a capability, behavior, or contract using an adjective phrase or noun phrase (Microsoft, 2025k; Microsoft, 2026b). The word `Interface` must not appear in the name.

#### Correct

```csharp
public interface IDamageable
{
}
```

The interface uses the `I` prefix and names a capability.

#### Incorrect

```csharp
public interface DamageableInterface
{
}
```

The name omits the required prefix and adds the redundant `Interface` suffix.

### 3.5 Enumerations

Enumeration names must use **PascalCase** singular nouns, except `[Flags]` bitmask enumerations, which must use plural nouns. The suffixes `Enum`, `Flag`, and `Flags` are prohibited (Microsoft, 2025k; Microsoft, 2026b).

#### Correct

```csharp
public enum WeaponType
{
    Sword,
    Bow,
    Staff
}
```

The singular PascalCase name represents one selection from the enumeration.

#### Incorrect

```csharp
public enum WeaponTypeEnum
{
    Sword,
    Bow,
    Staff
}
```

The `Enum` suffix repeats information already expressed by the type declaration.

### 3.6 Enumeration Members

Use PascalCase members without repeating the enumeration name as a prefix (Microsoft, 2025k).

#### Correct

```csharp
public enum TurnState
{
    Waiting,
    Active
}
```

The members are readable without repeating TurnState.

#### Incorrect

```csharp
public enum TurnState
{
    TurnStateWaiting,
    TurnStateActive
}
```

Each member repeats information already supplied by the enumeration type.

### 3.7 Fields

Use *_camelCase* for private or internal instance fields, PascalCase for static readonly fields, and s_camelCase for mutable private static fields; do not expose mutable fields (Microsoft, 2026b). As a layout convention, place constants first, followed by fields, constructors, properties, and methods.

#### Correct

```csharp
private const int MaximumLives = 3;
private readonly IPlayerRepository _players;
private int _remainingLives;
```

Constants and fields use their required names and precede behavioral members.

#### Incorrect

```csharp
private const int maximumLives = 3;
private readonly IPlayerRepository players;
public int RemainingLives;
```

The fields use inconsistent names and expose mutable state directly.

### 3.8 Properties

Property names must use **PascalCase** nouns, noun phrases, or adjectives. Boolean properties must use affirmative names prefixed with `Is`, `Has`, `Can`, or `Should` when doing so clarifies state (Microsoft, 2023g).

#### Correct

```csharp
public int CurrentHealth { get; private set; }

public bool IsShieldActive { get; private set; }
```

The properties use PascalCase and the Boolean name states an affirmative condition.

#### Incorrect

```csharp
public int current_health { get; private set; }

public bool NoShield { get; private set; }
```

The property casing is invalid and the negative Boolean name is harder to interpret.

### 3.9 Local Variables

Local variable names must use **camelCase** and clearly identify the stored value without arbitrary abbreviations or underscores (Microsoft, 2026b).

#### Correct

```csharp
int totalDamage = baseDamage + bonusDamage;
```

The local name uses camelCase and describes the calculated value.

#### Incorrect

```csharp
int Total_Dmg = baseDamage + bonusDamage;
```

The local name combines invalid casing with an unclear abbreviation.

### 3.10 Parameters

Method and constructor parameters must use **camelCase** and describe the semantic role of the argument rather than repeating only its data type (Microsoft, 2023h). Rules governing `out` and `ref` parameters are defined in Section 5.8.

#### Correct

```csharp
public void SetMovementSpeed(float movementSpeed)
{
}
```

The parameter name identifies its role at the call site.

#### Incorrect

```csharp
public void SetMovementSpeed(float ms)
{
}
```

The abbreviation does not explain what the numeric value controls.

### 3.11 Constants

Compile-time `const` identifiers must use **PascalCase** regardless of accessibility; `UPPER_SNAKE_CASE` is prohibited (Microsoft, 2026b).

#### Correct

```csharp
private const int MaximumLives = 3;
```

The constant uses the required PascalCase form.

#### Incorrect

```csharp
private const int MAXIMUM_LIVES = 3;
```

The constant uses a casing convention that this standard prohibits.

### 3.12 Methods

Use PascalCase verbs that describe the actual operation (Microsoft, 2023g). Include enough domain context to distinguish the action; verbs such as Get, Send, or Create are acceptable when the complete name explains their purpose.

#### Correct

```csharp
public int CountOccupiedCells(int[] board)
{
    return board.Count(cell => cell != EmptyCell);
}
```

The complete name identifies what the method counts.

#### Incorrect

```csharp
public int Execute(int[] board)
{
    return board.Count(cell => cell != EmptyCell);
}
```

The generic verb hides the operation performed.

### 3.13 Events

Event names must use **PascalCase** verb phrases. Use verb tense (such as `Closing` and `Closed`) to distinguish pre-action and post-action notifications; prefixes `Before` and `After` are prohibited (Microsoft, 2023g).

#### Correct

```csharp
public event EventHandler? PlayerDied;
```

The past-tense event name indicates that the event occurs after the action.

#### Incorrect

```csharp
public event EventHandler? AfterPlayerDeath;
```

The `After` prefix replaces the required event tense convention.

### 3.14 Test Classes

Name unit tests TypeUnderTestTests and integration tests FeatureIntegrationTests; use descriptive Stub or Fake suffixes for controlled test doubles (Microsoft, 2026o; Reese, 2025). These suffixes are naming conventions that make verification scope visible.

#### Correct

```csharp
[TestClass]
public sealed class DamageCalculatorTests
{
}
```

The name identifies the production type being tested.

#### Incorrect

```csharp
[TestClass]
public sealed class TestStuff
{
}
```

The name does not identify what the tests verify.

### 3.15 Test Methods

Use Functionality_Flow_ExpectedBehavior with PascalCase segments, omitting the redundant Test prefix (Reese, 2025). An additional segment is acceptable when it clarifies the expected result; do not replace meaningful behavior with a generic success label.

#### Correct

```csharp
public void TakeDamage_ValidAmount_DecreasesHealth()
{
}
```

The name states the functionality, exercised flow, and expected effect.

#### Incorrect

```csharp
public void TestDamage()
{
}
```

The name does not state the flow or expected behavior.

### 3.16 Prefixes and Suffixes

Use meaningful established affixes, including I for interfaces and Exception for custom exception types (Microsoft, 2025k, 2026c). Do not add redundant type markers. For asynchronous abstractions, omit Async consistently when no synchronous alternative exists; retain required framework or generated names, and distinguish synchronous and asynchronous alternatives when both exist (Microsoft, 2025a).

#### Correct

```csharp
public interface IInventory
{
}
```

The interface uses the established I prefix without a redundant type suffix.

#### Incorrect

```csharp
public interface InventoryInterface
{
}
```

The name replaces the established I prefix with the redundant Interface suffix.

### 3.17 WPF Controls, Resource Keys, and Client Asset Files

Use purpose-and-control-type camelCase for named WPF controls and PascalCase for views and styles. Use stable Feature_Purpose English localization keys and descriptive PascalCase asset filenames, without spaces or sequence numbers (Microsoft, 2026b). These naming conventions keep resources recognizable when display wording changes.

#### Correct

```text
usernameTextBox
SignIn_InvalidCredentialsMessage
DefaultProfilePicture.png
```

The names identify each control, resource purpose, and asset.

#### Incorrect

```text
txtUser
SignIn_PleaseTryAgainNow
default picture 1.png
```

The names use a control prefix, encode display wording, or fail the asset naming convention.

## 4. Code Style and Formatting

### 4.1 General Formatting

Write at most one statement and one variable declaration per line. Enforce shared formatting rules consistently across contributors through a repository `.editorconfig` file (Microsoft, 2025f).

#### Correct

```csharp
int currentHealth = player.CurrentHealth;
int maximumHealth = player.MaximumHealth;

currentHealth -= receivedDamage;
UpdateHealthBar(currentHealth, maximumHealth);
```

Each declaration and statement occupies its own line.

#### Incorrect

```csharp
int currentHealth = player.CurrentHealth, maximumHealth = player.MaximumHealth; currentHealth -= receivedDamage; UpdateHealthBar(currentHealth, maximumHealth);
```

Multiple declarations and statements are compressed onto one line.

### 4.2 Indentation

Indent each block level using **four spaces**; tab characters are prohibited. Indent continuation lines by at least four spaces relative to the starting statement (Microsoft, 2025f).

#### Correct

```csharp
public void UpdatePlayer()
{
    if (IsAlive)
    {
        MovePlayer();
        UpdateAnimation();
    }
}
```

Every nested block uses four additional spaces.

#### Incorrect

```csharp
public void UpdatePlayer()
{
  if (IsAlive)
  {
      MovePlayer();
   UpdateAnimation();
  }
}
```

The block uses inconsistent two-space and three-space indentation.

### 4.3 Braces

Use Allman braces for control-flow blocks and handwritten member bodies; never omit braces around a single statement (Microsoft, 2023a, 2025f). Auto-implemented accessors such as { get; set; } remain permitted; expression-bodied members are prohibited, while lambda and switch-expression arrows follow Sections 5.12 and 5.14.

#### Correct

```csharp
public void CloseSession()
{
    if (_isClosed)
    {
        return;
    }

    _isClosed = true;
}
```

Both the method and its guard use explicit Allman blocks.

#### Incorrect

```csharp
public void CloseSession()
{
    if (_isClosed) return;
    _isClosed = true;
}
```

The guard omits the required block and compresses its return onto the condition line.

### 4.4 Line Length

Source lines must not exceed **100 characters** except for indivisible string literals, long URLs, or tool-generated files (Google, n.d.; Microsoft, 2025f).

#### Correct

```csharp
PlayerAttackResult powerAttackResult = combatSystem.CalculateSpecialAttack(
    activePlayer,
    equippedWeapon,
    targetedEnemy);
```

The invocation is wrapped so every source line remains within the limit.

#### Incorrect

```csharp
PlayerAttackResult powerAttackResult = combatSystem.CalculateSpecialAttack(activePlayer, equippedWeapon, targetedEnemy);
```

The invocation exceeds the line-length limit without an indivisible reason.

### 4.5 Line Wrapping

Break long statements at logical syntactic boundaries, indent continuation lines by four spaces, and break compound expressions **before** binary operators such as `&&`, `||`, and `+` (Google, n.d.; Microsoft, 2025f).

#### Correct

```csharp
bool canPerformAttack =
    player.IsAlive
    && player.HasWeapon
    && player.Stamina >= MinimumAttackStamina;

PerformAttack(
    player,
    selectedWeapon,
    targetEnemy);
```

The continuation indentation preserves the structure of both expressions.

#### Incorrect

```csharp
bool canPerformAttack = player.IsAlive &&
player.HasWeapon && player.Stamina >=
MinimumAttackStamina;

PerformAttack(player,
selectedWeapon, targetEnemy);
```

The arbitrary breaks obscure expression boundaries and continuation levels.

### 4.6 Horizontal Whitespace

Apply horizontal spacing consistently (Microsoft, 2025c):

- One space after control-flow keywords (`if`, `for`, `foreach`, `while`, `switch`, `catch`, `lock`) and after commas.
- One space on both sides of binary and ternary operators (`=`, `+`, `-`, `==`, `!=`, `&&`, `||`, `?`, `:`).
- No space between a method name and `(`, inside parentheses `(x, y)`, before commas, around `.`, or trailing at line ends.

#### Correct

```csharp
if (currentHealth > 0 && hasControl)
{
    playerPosition = CalculatePosition(coordinateX, coordinateY);
}
```

The spaces separate keywords, operators, and arguments consistently.

#### Incorrect

```csharp
if(currentHealth>0&&hasControl)
{
    playerPosition=CalculatePosition (coordinateX ,coordinateY);
}
```

The missing and misplaced spaces make the expression harder to scan.

### 4.7 Vertical Whitespace

Separate method and property declarations with one blank line, and use single blank lines inside methods to separate distinct logical phases. Consecutive blank lines and blank lines immediately inside braces are prohibited (Microsoft, 2025f).

#### Correct

```csharp
public void TakeDamage(int damage)
{
    CurrentHealth = Math.Max(CurrentHealth - damage, 0);

    UpdateHealthBar();
}

public void RestoreHealth()
{
    CurrentHealth = MaximumHealth;

    UpdateHealthBar();
}
```

Single blank lines separate logical phases and member declarations.

#### Incorrect

```csharp
public void TakeDamage(int damage)
{

    CurrentHealth = Math.Max(CurrentHealth - damage, 0);


    UpdateHealthBar();
}
public void RestoreHealth()
{
    CurrentHealth = MaximumHealth;
    UpdateHealthBar();
}
```

The example contains excessive blank lines and no separation between methods.

### 4.8 Using Directives

Place using directives outside the namespace, ordered alphabetically with System namespaces first, and remove unused imports (Microsoft, 2022a, 2023i). Put type aliases last and use them only to resolve name collisions, not as arbitrary abbreviations.

#### Correct

```csharp
using System;
using System.Collections.Generic;
using GameProject.Inventory;

namespace GameProject.Client;
```

The imports precede the namespace and place System namespaces first.

#### Incorrect

```csharp
namespace GameProject.Client;

using GameProject.Inventory;
using System.Collections.Generic;
using System;
```

The imports appear after the namespace and are not ordered consistently.

### 4.9 Namespace Declarations

Every source file that declares types must declare an explicit namespace matching its logical module; files containing only assembly attributes do not need one. Prefer **file-scoped namespaces** (`namespace GameProject.Gameplay.Combat;`) unless a file legitimately requires multiple namespaces (Microsoft, 2026h).

#### Correct

```csharp
using System;

namespace GameProject.Gameplay.Combat;

public sealed class DamageCalculator
{
    public int CalculateDamage(int baseDamage)
    {
        return Math.Max(baseDamage, 0);
    }
}
```

The file-scoped namespace matches the module without adding indentation.

#### Incorrect

```csharp
namespace GameProject
{
    namespace Gameplay.Combat
    {
        public sealed class DamageCalculator
        {
        }
    }
}
```

Nested namespace blocks add structure that a file-scoped declaration expresses directly.

### 4.10 Early Returns and Final Assignment

Use early returns for initial guards so the main operation stays at base indentation (JetBrains, 2026). For the main decision, prefer assigning the output and returning once when that makes the flow clearer; never bypass required updates or cleanup.

#### Correct

```csharp
public int CalculateReward(bool hasBonus)
{
    int reward = BaseReward;
    if (hasBonus)
    {
        reward += BonusReward;
    }

    return reward;
}
```

The method keeps one readable output assignment without adding redundant exits.

#### Incorrect

```csharp
public int CalculateReward(bool hasBonus)
{
    if (hasBonus)
    {
        return BaseReward + BonusReward;
    }
    else
    {
        return BaseReward;
    }
}
```

The else and separate returns add structure to a simple value calculation.

## 5. C# Programming Practices

### 5.1 Access Modifiers

Declare explicit access modifiers where the language permits them and select the narrowest visibility required by callers; do not widen access only to simplify tests (Ecma International, 2023).

#### Correct

```csharp
private int CountOccupiedCells(int[] board)
{
    return board.Count(cell => cell != EmptyCell);
}
```

The helper explicitly limits access to its owning type.

#### Incorrect

```csharp
int CountOccupiedCells(int[] board)
{
    return board.Count(cell => cell != EmptyCell);
}
```

The helper relies on an implicit access modifier.

### 5.2 Auto-Implemented Properties and Property Accessors

Use auto-implemented properties when accessors need no validation or state coordination, exposing only the access each caller needs (Microsoft, 2025l). Use a backing field only when an accessor performs meaningful work.

#### Correct

```csharp
public string DisplayName { get; set; } = string.Empty;
```

The property needs no backing field because its accessors have no additional behavior.

#### Incorrect

```csharp
private string _displayName = string.Empty;

public string DisplayName
{
    get
    {
        return _displayName;
    }
    set
    {
        _displayName = value;
    }
}
```

The explicit field and accessors repeat the behavior of an auto-implemented property.

### 5.3 Immutability and readonly Usage

Use readonly fields for dependencies and state that must not be reassigned after construction (Microsoft, 2023b, 2026a). A readonly reference does not make its object immutable; keep gameplay state mutable when the behavior requires it.

#### Correct

```csharp
private readonly IPlayerRepository _players;
```

The dependency reference cannot be replaced after construction.

#### Incorrect

```csharp
private IPlayerRepository _players;
```

The dependency remains replaceable without a behavioral need.

### 5.4 Constants and Magic Values

Replace unexplained domain numbers and strings with named `const`, `static readonly`, or configuration values (Google, n.d.). Structural literals such as `0` and `1` may remain inline when their mathematical or indexing role is self-evident.

#### Correct

```csharp
private const int MaximumComboMultiplier = 5;

public bool HasMaximumCombo(int comboMultiplier)
{
    return comboMultiplier >= MaximumComboMultiplier;
}
```

The named constant explains the domain limit at the comparison site.

#### Incorrect

```csharp
public bool HasMaximumCombo(int comboMultiplier)
{
    return comboMultiplier >= 5;
}
```

The literal does not explain why five is the relevant threshold.

### 5.5 Method Responsibility

Apply Single Responsibility and Keep It Simple (KISS): keep each operation cohesive and prefer the simplest design that satisfies its behavior (Microsoft, 2023c). Do not add wrappers, types, or layers that only pass an existing operation onward.

#### Correct

```csharp
public void TakeDamage(int damage)
{
    CurrentHealth = Math.Max(CurrentHealth - damage, 0);
}
```

The method changes only the health state it owns.

#### Incorrect

```csharp
public void TakeDamage(int damage)
{
    CurrentHealth = Math.Max(CurrentHealth - damage, 0);
    _repository.SaveHealth(CurrentHealth);
    _soundPlayer.PlayDamageSound();
}
```

The method mixes health rules with storage and sound playback.

### 5.6 Don't Repeat Yourself (DRY)

Keep each shared rule in one authoritative implementation and call it directly when its meaning is identical; similar code may remain separate when it represents different concepts (Microsoft, 2023c, 2026e).

#### Correct

```csharp
public int CalculateDamageAfterArmor(int damage, int armor)
{
    return Math.Max(damage - armor, MinimumDamage);
}
```

One method owns the calculation and can serve all callers directly.

#### Incorrect

```csharp
public int CalculatePlayerDamage(int damage, int armor)
{
    return Math.Max(damage - armor, MinimumDamage);
}

public int CalculateEnemyDamage(int damage, int armor)
{
    return Math.Max(damage - armor, MinimumDamage);
}
```

Two methods independently repeat the same damage rule.

### 5.7 Method Size

Treat method length as a review signal rather than a reason to split code automatically; extract only helpers with a clear responsibility (Esposito, 2014). Keep a longer linear method when extraction would hide its flow.

#### Correct

```csharp
public void FinishTurn()
{
    _match.AdvanceTurn();
    _match.RestartTurnClock();
}
```

The related turn updates remain visible in one short operation.

#### Incorrect

```csharp
public void FinishTurn()
{
    Advance();
    Restart();
}

private void Advance()
{
    _match.AdvanceTurn();
}

private void Restart()
{
    _match.RestartTurnClock();
}
```

The helpers merely rename existing calls and force readers to jump between methods.

### 5.8 Parameters

Keep signatures readable and group values only when they form a real concept (Microsoft, 2023h). As a convention, three or more parameters prompt review, not mandatory wrapping. Do not declare out parameters in owned APIs; framework Try calls are allowed. Use ref only for required framework signatures or atomic operations, never for application-defined WCF outputs.

#### Correct

```csharp
public void PlacePiece(PiecePlacement placement)
{
    _board.Place(placement.Row, placement.Column);
}
```

The parameter groups coordinates that describe one piece placement.

#### Incorrect

```csharp
public bool PlacePiece(int row, int column, out string error)
{
    error = string.Empty;
    _board.Place(row, column);
    return true;
}
```

The owned API exposes an output parameter instead of a clear return contract.

### 5.9 Cyclomatic Complexity

Review methods above a cyclomatic complexity of 10, a team review threshold rather than a language limit (Microsoft, 2023d, 2025e). Reduce unrelated branches without creating pass-through helpers; a readable flat mapping may justify retaining more cases.

#### Correct

```csharp
public bool CanMove(Player player)
{
    return player.IsAlive && !player.IsStunned;
}
```

One Boolean expression directly states the movement condition.

#### Incorrect

```csharp
public bool CanMove(Player player)
{
    bool canMove = false;
    if (player.IsAlive)
    {
        if (!player.IsStunned)
        {
            canMove = true;
        }
    }

    return canMove;
}
```

Nested branches obscure a condition that needs no separate control-flow blocks.

### 5.10 Nullable Reference Types

Enable nullable reference analysis and use ? only when absence is intentional; do not use the null-forgiving operator to hide missing initialization (Microsoft, 2026i).

#### Correct

```csharp
public string DisplayName { get; set; } = string.Empty;

public string? ClanTag { get; set; }
```

Required text is initialized while the optional clan tag explicitly permits absence.

#### Incorrect

```csharp
public string DisplayName { get; set; } = null!;
```

The null-forgiving operator suppresses a warning without establishing valid state.

### 5.11 Null Handling

Do not return null as an ambiguous sentinel; use an empty collection or an explicit status when appropriate (Microsoft, 2026i, 2026l). Use ?? for a valid optional fallback, ?? throw for required arguments, and single-level ?. only when skipping the operation is safe; avoid deep chains that conceal missing required state.

#### Correct

```csharp
public PlayerService(IPlayerRepository players)
{
    _players = players ?? throw new ArgumentNullException(nameof(players));
}
```

The constructor immediately rejects its missing required dependency.

#### Incorrect

```csharp
public PlayerService(IPlayerRepository? players)
{
    _players = players;
}
```

The constructor accepts a missing dependency without defining valid behavior.

### 5.12 Pattern Matching

Use declaration patterns to check a type and obtain its value without a separate cast; `is null` and `is not null` explicitly test absence (Microsoft, 2023e, 2024). Simple and/or patterns and flat switch expressions are allowed when clearer than alternatives; avoid nested patterns and keep multi-condition domain rules in named methods. Switch-arm arrows are not expression-bodied members.

#### Correct

```csharp
if (sender is Button button)
{
    button.IsEnabled = false;
}
```

The pattern checks the type and supplies the typed variable in one operation.

#### Incorrect

```csharp
if (sender is Button)
{
    Button button = (Button)sender;
    button.IsEnabled = false;
}
```

The code repeats a type conversion already implied by the check.

### 5.13 LINQ Usage

Use LINQ method syntax for readable, side-effect-free queries; filter before materializing and use AsNoTracking for read-only Entity Framework queries (Microsoft, 2020a, 2020b, 2025j). Materialize shared mutable collections under their owning lock; copy mutable elements when an isolated snapshot is required.

#### Correct

```csharp
Player[] activePlayers = players
    .Where(player => player.IsActive)
    .ToArray();
```

The query filters the source before materializing its result.

#### Incorrect

```csharp
Player[] activePlayers = players
    .ToArray()
    .Where(player => player.IsActive)
    .ToArray();
```

The query copies the complete source before applying its filter.

### 5.14 Lambda Expressions

Use short lambdas for side-effect-free predicates and projections, or simple callbacks with at most two statements (Microsoft, 2026f). Side effects belong in explicit callbacks, not query predicates; give branching or reused logic a descriptive method name.

#### Correct

```csharp
Player[] activePlayers = players.Where(player => player.IsActive).ToArray();
```

The predicate only checks the property used to select players.

#### Incorrect

```csharp
Player[] activePlayers = players.Where(player =>
{
    player.RefreshActivity();
    return player.IsActive;
}).ToArray();
```

The query predicate unexpectedly changes each player while filtering.

### 5.15 Events and Delegates

Use event for notifications so subscribers cannot replace or invoke the publisher delegate (Microsoft, n.d.-b). EventHandler is the recommended .NET event pattern. As an adopted convention, notifications used only within the application may use Action without artificial EventArgs types, regardless of their access modifier. Use Func for callbacks that return a value and named handlers when later unsubscription is required (Microsoft, 2021d).

#### Correct

```csharp
public event Action<long>? PlayerConnected;
```

The event allows subscription while retaining invocation control in its owner.

#### Incorrect

```csharp
public Action<long>? PlayerConnected;
```

The public delegate field can be replaced or invoked by callers.

### 5.16 Resource Disposal

Dispose locally owned IDisposable resources using `using` or a deterministic finally block; do not dispose a borrowed dependency unless ownership is transferred (Microsoft, 2025d, 2026n).

#### Correct

```csharp
using MemoryStream stream = new MemoryStream();
serializer.WriteObject(stream, state);
```

The owned stream is disposed even if serialization fails.

#### Incorrect

```csharp
MemoryStream stream = new MemoryStream();
serializer.WriteObject(stream, state);
stream.Dispose();
```

An exception during serialization bypasses the explicit disposal.

### 5.17 Serialization

Create DTOs for actual transport boundaries, reusing meaningful internal models between internal layers (Microsoft, 2021e). For WCF, use explicit DataContract and DataMember attributes, including EnumMember on serialized enum values; preserve existing member order and validate received data (Microsoft, 2021a, 2021b).

#### Correct

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerSummary
{
    [DataMember(Order = 1)]
    public string DisplayName { get; set; } = string.Empty;
}
```

The attributes explicitly identify the transport type and stable member order.

#### Incorrect

```csharp
public sealed class PlayerSummary
{
    public string DisplayName { get; set; } = string.Empty;
}
```

The type provides no explicit WCF data-contract definition.

### 5.18 Explicit Local Variable Types

Declare all local variables with an **explicit type** in both production and test code (`IDE0008`); the `var` keyword and target-typed `new()` expressions are prohibited (Microsoft, 2022b; Microsoft, 2025f).

#### Correct

```csharp
PlayerController playerController = new PlayerController();
List<Enemy> activeEnemies = new List<Enemy>();
int remainingLives = player.RemainingLives;
```

Each local declaration states its complete type.

#### Incorrect

```csharp
var playerController = new PlayerController();
List<Enemy> activeEnemies = new();
var remainingLives = player.RemainingLives;
```

The declarations rely on `var` and target-typed construction prohibited by this standard.

### 5.19 Conditional Operator

Use a single flat conditional operator (`condition ? consequent : alternative`) only when one clear Boolean condition selects between two short expressions of the same conceptual kind (Ecma International, 2023). Nested conditional operators, multi-statement branches, and side-effect selection are prohibited. Wrap long expressions with `?` and `:` aligned on continuation lines (Section 4.5).

#### Correct

```csharp
public double CalculateWinRatio(int wins, int sessionsPlayed)
{
    return sessionsPlayed == 0
        ? 0d
        : (double)wins / sessionsPlayed;
}
```

One condition selects between two short values of the same kind.

#### Incorrect

```csharp
public RoundOutcome ResolveOutcome(int teamAlphaScore, int teamBravoScore)
{
    return teamAlphaScore > teamBravoScore
        ? RoundOutcome.AlphaVictory
        : teamAlphaScore == teamBravoScore
            ? RoundOutcome.Draw
            : RoundOutcome.BravoVictory;
}
```

The nested conditional requires the reader to resolve a second branch inside the first.

### 5.20 Thread Synchronization and Concurrency

Protect related shared-state updates with a short lock on a dedicated private readonly object, using a static readonly object for static state (Microsoft, n.d.-e). The object identifies the monitor; it holds no business data. Use object rather than System.Threading.Lock for framework compatibility, and never await or perform remote work inside the block.

#### Correct

```csharp
lock (_syncRoot)
{
    _players.Add(player);
    _playerCount++;
}
```

The dedicated monitor keeps the related collection and count updates together.

#### Incorrect

```csharp
lock (this)
{
    _players.Add(player);
    _playerCount++;
}
```

Other code can lock the public instance and interfere with its synchronization.

### 5.21 Semaphores and Asynchronous Mutual Exclusion

Use SemaphoreSlim for bounded concurrent access or exclusion that spans await; a permit count of one provides mutual exclusion (Microsoft, n.d.-a, n.d.-g). Use named limits, release only an acquired permit in finally, and dispose an owned semaphore after its users finish.

#### Correct

```csharp
await _requests.WaitAsync(cancellationToken);
try
{
    await _channel.RefreshState();
}
finally
{
    _requests.Release();
}
```

The acquired permit is released even when the remote operation fails.

#### Incorrect

```csharp
await _requests.WaitAsync(cancellationToken);
await _channel.RefreshState();
_requests.Release();
```

An exception can skip the release and block later callers.

### 5.22 Atomic Operations

Use Interlocked for one atomic shared-field update; related operations that preserve one invariant still need coordinated synchronization (Microsoft, n.d.-c). Do not add multiple synchronization mechanisms for the same already-protected invariant.

#### Correct

```csharp
int count = Interlocked.Increment(ref _connectedPlayerCount);
```

The increment and returned value form one atomic operation.

#### Incorrect

```csharp
_connectedPlayerCount++;
int count = _connectedPlayerCount;
```

The increment and later read can interleave with other threads.

### 5.23 Concurrent Collections

Use concurrent collections for independent operations performed by multiple threads; sequences spanning several operations still require synchronization when they preserve one invariant (Microsoft, n.d.-h).

#### Correct

```csharp
bool added = _sessions.TryAdd(token, session);
```

ConcurrentDictionary performs the attempted insertion atomically.

#### Incorrect

```csharp
bool added = !_sessions.ContainsKey(token);
if (added)
{
    _sessions[token] = session;
}
```

Another caller can change the collection between the existence check and assignment.

### 5.24 You Aren't Gonna Need It (YAGNI)

Implement only abstractions and options required by current behavior; introduce extension points when a real requirement justifies them (Microsoft, 2023c).

#### Correct

```csharp
public bool IsTurnExpired(DateTime turnStartedAtUtc, DateTime utcNow)
{
    return utcNow - turnStartedAtUtc >= _turnDuration;
}
```

The method implements only the turn-expiration rule currently required.

#### Incorrect

```csharp
public bool IsTurnExpired(DateTime turnStartedAtUtc, DateTime utcNow)
{
    if (_enableFutureGracePeriod)
    {
        return utcNow - turnStartedAtUtc >= _turnDuration + _futureGracePeriod;
    }

    return utcNow - turnStartedAtUtc >= _turnDuration;
}
```

The branch adds an unused option for behavior that has not been requested.

### 5.25 Local Functions

Use a local function to group a sequence of work exclusive to one operation when keeping the steps together makes the flow clearer (Microsoft, n.d.-d). Use explicit block bodies, avoid deep nesting, and keep shared behavior in an ordinary method. Do not introduce local functions that merely hide a simple expression or rename an existing call.

#### Correct

```csharp
public Task<int> ChangeTurnDuration(int seconds)
{
    async Task<int> ApplyDurationChange()
    {
        await _channel.Open();
        int acceptedSeconds = await _channel.ChangeTurnDuration(seconds);
        return acceptedSeconds;
    }

    return _communication.HandleFailures(ApplyDurationChange);
}
```

The local function groups channel preparation and the remote update into one operation passed to the shared failure handler.

#### Incorrect

```csharp
public int ReadScore(Player player)
{
    int ReadValue()
    {
        return player.Score;
    }

    return ReadValue();
}
```

The local function only hides an already clear property access.

### 5.26 Asynchronous Operations

Use Task and await for real asynchronous work, keeping UI threads responsive; keep simple calculations synchronous (Microsoft, 2025a). Do not wrap asynchronous I/O in Task.Run, add async without needed awaiting, or block the UI with Result or Wait. Keep async void only for framework event handlers that require it.

#### Correct

```csharp
public int CountPlayers(Player[] players)
{
    return players.Length;
}
```

The immediate calculation needs no asynchronous machinery.

#### Incorrect

```csharp
public Task<int> CountPlayers(Player[] players)
{
    return Task.Run(() => players.Length);
}
```

The task schedules background work for an immediate collection count.

## 6. Error Handling, Logging, and Diagnostics

### 6.1 Exception Handling

Return explicit statuses for expected validation or domain rejections; let unexpected technical failures reach the responsible error boundary (Microsoft, 2025b). Catch specific exceptions only for meaningful local recovery, preserve the stack with throw;, and keep exception filters side-effect-free. Broad catches belong only at explicit last-chance boundaries, not as substitutes for normal results.

#### Correct

```csharp
try
{
    channel.Close();
}
catch (CommunicationException)
{
    channel.Abort();
}
```

The specific catch performs the recovery required for a failed channel close.

#### Incorrect

```csharp
try
{
    channel.Close();
}
catch (Exception)
{
}
```

The broad empty catch hides every failure without recovery or diagnostics.

### 6.2 Custom Exceptions

Prefer standard .NET exceptions for programming errors or technical failures (Microsoft, 2025b). Define a custom exception only when callers must distinguish a failure that standard types cannot express; normal user-input rejection remains an explicit status.

#### Correct

```csharp
if (players == null)
{
    throw new ArgumentNullException(nameof(players));
}
```

The standard exception precisely identifies a missing required programming argument.

#### Incorrect

```csharp
if (players == null)
{
    throw new MissingPlayersException();
}
```

The custom exception duplicates the meaning of ArgumentNullException.

### 6.3 Logging

Emit diagnostics through an injected ILogger stored in a readonly field, configuring providers at executable composition roots (Microsoft, 2026g). Released server builds need persistent output; application classes must not bypass the logging pipeline with Console or Debug writes. A logger does not justify an arbitrary parameter-count exception.

#### Correct

```csharp
_logger.LogInformation("Match {MatchId} started.", matchId);
```

The diagnostic follows the configured structured logging pipeline.

#### Incorrect

```csharp
Console.WriteLine("Match " + matchId + " started.");
```

The direct console write bypasses configured providers and filtering.

### 6.4 Log Levels

Choose log levels by technical impact and record an unexpected failure once at the boundary responsible for it (Microsoft, 2026g).

| Level | Usage |
| --- | --- |
| Trace / Debug | Detailed development diagnostics, disabled by default in released builds. |
| Information | Significant lifecycle events. |
| Warning | Abnormal or recoverable conditions where execution can continue safely. |
| Error | An unexpected operation failure while the process continues serving other operations. |
| Critical | Process-threatening failures, startup aborts, or data-integrity hazards. |

#### Correct

```csharp
_logger.LogWarning("Connection interrupted; attempting recovery.");
```

The warning describes a recoverable interruption.

#### Incorrect

```csharp
_logger.LogCritical("Player opened the settings view.");
```

A routine UI action does not indicate a process-threatening failure.

### 6.5 Log Message Conventions

Use static English templates with PascalCase placeholders, preserving correlation identifiers across boundaries (Microsoft, 2026d, 2026g). Pass exceptions through the logger exception parameter and use sanitizing providers; never record credentials, session tokens, verification codes, connection strings, or unredacted personal data.

#### Correct

```csharp
_logger.LogError(exception, "Failed to save match {MatchId}.", matchId);
```

The static template preserves structured context and the exception for safe diagnostics.

#### Incorrect

```csharp
_logger.LogError($"Failed to save match {matchId}: {exception.Message}");
```

Interpolation discards structured exception information and may expose sensitive details.

### 6.6 Player-Facing Error Messages

Present localized, specific, user-centered, and actionable messages to players without exposing exception type names, stack traces, SQL/storage errors, or physical file paths (Microsoft, 2025h).

#### Correct

```text
Your saved game could not be loaded because the file is damaged.
Start a new game or restore a previous backup.
```

The message explains the problem and gives the player a useful next action.

#### Incorrect

```text
System.IO.InvalidDataException in SaveSlotReader.ReadHeader():
Unexpected end of stream at offset 0x000014A2.
```

The message exposes implementation details that do not help the player recover.

### 6.7 Unhandled Exceptions and Diagnostic Reporting

Install last-chance executable hooks and a centralized WCF IErrorHandler (Microsoft, 2021c). Preserve declared contractual faults, sanitize unexpected faults, and retain one correlation identifier through fault creation and logging; background timers and callbacks need their own boundaries. HandleError logging must not imply that corrupted state can safely continue.

#### Correct

```csharp
if (error is FaultException<ServiceFault>)
{
    return;
}
```

The handler preserves an already constructed contractual service fault.

#### Incorrect

```csharp
throw new FaultException<string>(error.ToString());
```

The fault exposes internal exception details to the remote caller.

## 7. Comments and Documentation

### 7.1 General Comment Rules

When a comment is necessary, write a short English sentence above the code and explain information the declaration cannot express; omit comments that merely repeat it (Microsoft, 2025f).

#### Correct

```text
Copy the board before publishing it so later moves cannot change the sent state.
```

The text explains the reason for copying the board.

#### Incorrect

```text
Copy the board.
```

The text only repeats the operation name.

### 7.2 Implementation Comments

Document only non-obvious constraints or workarounds, avoiding line-by-line narration and block-comment essays (Microsoft, 2025f). Prefer clearer names or simpler code when those remove the need for an explanation.

#### Correct

```text
Keep the turn clock running during reconnection so network loss cannot pause play.
```

The text records a behavioral constraint that might otherwise be changed accidentally.

#### Incorrect

```text
Read the clock, compare the time, and update the turn.
```

The text narrates implementation steps rather than explaining their reason.

### 7.3 XML Documentation Comments

Use well-formed XML with an informative summary and applicable parameter, return, and exception descriptions matching the signature (Microsoft, 2025g, 2026j).

#### Correct

```xml
<summary>Returns the number of occupied cells on the board.</summary>
```

The XML summary describes the meaning of the returned value.

#### Incorrect

```xml
<sumary>Counts cells.</sumary>
```

The misspelled XML tag is not a recognized summary element.

### 7.4 Public API Documentation

Document externally consumed service or library contracts and any non-obvious side effects or failures (Microsoft, 2025i). Do not require repetitive XML documentation on internal implementation types or tests merely because they are public; an implementation may inherit its interface documentation.

#### Correct

```text
Only the creator can cancel a waiting room.
```

The documentation states an authorization constraint that the method name cannot express.

#### Incorrect

```text
Provides room cancellation methods.
```

The documentation repeats the API name without describing its contract.

### 7.5 TODO Comments

Use TODO followed by a specific deferred action, and remove it when the work is complete; avoid vague reminders (Microsoft, 2025m). The examples show the note content without embedding a comment in code.

#### Correct

```text
TODO: Replace the local image path with the stored profile-image reference.
```

The note identifies a concrete change that a contributor can implement.

#### Incorrect

```text
TODO: Finish later.
```

The note does not identify the missing work.

## 8. Input Validation and Security

### 8.1 Trust Boundaries

Validate identity and permissions on the server using the authenticated session, never a caller-supplied account identifier (Microsoft, 2023j; OWASP Foundation, n.d.-b). Client checks improve usability but cannot replace authoritative authorization before access or mutation.

#### Correct

```csharp
long accountId = authenticatedSession.AccountId;
```

The operation obtains caller identity from the server-validated session.

#### Incorrect

```csharp
long accountId = request.AccountId;
```

The operation trusts an identity chosen by the remote caller.

### 8.2 Input Validation

Validate presence, bounds, format, and domain invariants before dependent operations (OWASP Foundation, n.d.-b; The MITRE Corporation, n.d.). Apply field-specific normalization without truncating invalid input or modifying passwords; return expected rejection statuses and allow unexpected infrastructure failures to propagate.

#### Correct

```csharp
if (content.Length > MaximumMessageLength)
{
    return MessageStatus.TooLong;
}
```

The validation explicitly rejects text above the supported limit.

#### Incorrect

```csharp
content = content.Substring(0, Math.Min(content.Length, MaximumMessageLength));
```

The code silently alters invalid input instead of reporting its rejection.

### 8.3 File System Access

Store uploaded files beneath a server-controlled root using generated filenames and allowlisted extensions; never trust client paths (Microsoft, n.d.-f; OWASP Foundation, n.d.-a). Check uploaded size and actual media content before storage, and do not expose physical server paths.

#### Correct

```csharp
string fileName = Guid.NewGuid().ToString("N") + PngExtension;
string path = Path.Combine(_imageRoot, fileName);
```

The filename is server-generated and cannot contain caller-controlled path segments.

#### Incorrect

```csharp
string path = Path.Combine(_imageRoot, request.FileName);
```

The caller-controlled name can redirect access outside the intended directory.

### 8.4 Credential and Secret Management

Read required secrets from environment settings or untracked local configuration and fail clearly when they are absent; never supply predictable fallback keys (OWASP Foundation, n.d.-c, n.d.-d). Hash passwords with a salted password-hashing algorithm, protect verification codes with purpose-bound keyed digests, and revoke and rotate exposed credentials.

#### Correct

```csharp
string secret = EnvironmentSettings.ReadRequired(SecretVariableName);
```

The required secret comes from external configuration without a fallback.

#### Incorrect

```csharp
string secret = Environment.GetEnvironmentVariable(SecretVariableName) ?? DefaultSecret;
```

The fallback hides missing configuration and substitutes a predictable secret.

## 9. Testing and Quality Assurance

### 9.1 Testing Strategy

Verify behavior at the lowest practical level: isolated rules use unit tests, and real component interactions use integration tests (Microsoft, 2026k; Reese, 2025). Manual UI checks complement automation rather than replacing verifiable regression coverage.

#### Correct

```text
Turn-rule calculation: isolated unit test.
Repository read/write: integration test with disposable data.
```

Each behavior is verified at the level that owns its correctness.

#### Incorrect

```text
Turn-rule calculation: open the client and check whether the board looks right.
```

The manual observation provides no isolated and repeatable rule verification.

### 9.2 Unit Testing

Use Microsoft.NET.Test.Sdk, MSTest.TestAdapter, and MSTest.TestFramework with TestClass and TestMethod so tests are individually discoverable in Visual Studio and dotnet test (Microsoft, 2026o, 2026m). Keep unit tests deterministic and isolated with controlled doubles, and compare explicit expected and actual values (Reese, 2025).

#### Correct

```csharp
[TestClass]
public sealed class ScoreCalculatorTests
{
    [TestMethod]
    public void CalculateTotal_NoScores_ReturnsZero()
    {
        int[] scores = Array.Empty<int>();
        ScoreCalculator calculator = new ScoreCalculator();
        int expectedScore = 0;

        int actualScore = calculator.CalculateTotal(scores);

        Assert.AreEqual(expectedScore, actualScore);
    }
}
```

The discoverable test verifies one isolated calculation with explicit expected and actual values.

#### Incorrect

```csharp
public sealed class ScoreCalculatorTests
{
    public void CalculateTotal_NoScores_ReturnsZero()
    {
        int[] scores = Array.Empty<int>();
        ScoreCalculator calculator = new ScoreCalculator();
        int expectedScore = 0;

        int actualScore = calculator.CalculateTotal(scores);

        Assert.AreEqual(expectedScore, actualScore);
    }
}
```

Without the MSTest attributes the check is not individually discoverable.

### 9.3 Integration Testing

Exercise the real boundary being tested and clean up disposable data or files deterministically (Microsoft, 2026k; van der Til et al., 2026). Keep one observable interaction per test; a save-and-load sequence can verify one persistence behavior.

#### Correct

```csharp
[TestMethod]
public void WriteAndRead_EmptyText_PreservesContent()
{
    string path = Path.GetTempFileName();
    string expectedContent = string.Empty;
    try
    {
        File.WriteAllText(path, expectedContent);
        string actualContent = File.ReadAllText(path);

        Assert.AreEqual(expectedContent, actualContent);
    }
    finally
    {
        File.Delete(path);
    }
}
```

The test verifies one real storage interaction and deletes its temporary file.

#### Incorrect

```csharp
[TestMethod]
public void WriteAndRead_EmptyText_PreservesContent()
{
    string path = Path.GetTempFileName();
    string expectedContent = string.Empty;

    File.WriteAllText(path, expectedContent);
    string actualContent = File.ReadAllText(path);

    Assert.AreEqual(expectedContent, actualContent);
}
```

The test leaves its temporary file behind instead of cleaning it deterministically.

### 9.4 Atomic Test Structure

Make each test verify one behavior in one flow, separating preparation, execution, and verification with blank lines rather than comments (Reese, 2025). Prefer one focused assertion; additional assertions must describe the same outcome, not unrelated scenarios. State expected and actual values clearly and use separate tests or data rows for different cases.

#### Correct

```csharp
[TestMethod]
public void CountPlayers_EmptyCollection_ReturnsZero()
{
    Player[] players = Array.Empty<Player>();
    int expectedCount = 0;

    int actualCount = players.Length;

    Assert.AreEqual(expectedCount, actualCount);
}
```

The test has one outcome and visibly separates preparation, execution, and verification.

#### Incorrect

```csharp
[TestMethod]
public void CountPlayers_EmptyCollection_ReturnsZero()
{
    Player[] players = Array.Empty<Player>();
    int expectedCount = 0;
    int actualCount = players.Length;
    Assert.AreEqual(expectedCount, actualCount);
    Assert.IsTrue(_connection.IsOnline);
}
```

The connection assertion adds an unrelated behavior to the collection-count test.

## 10. Version Control and Collaboration

### 10.1 Commit Messages and Atomicity

Make each Git commit **atomic and cohesive** by addressing one logical change. Write commit messages in **English** using the imperative mood and the Conventional Commits format `type(scope): summary`. Keep the subject at 72 characters or fewer (Chacon & Straub, 2014; Conventional Commits, 2020).

#### Correct

```text
feat(combat): calculate stamina cost for heavy melee attacks

Deduct weapon weight multipliers from available player stamina before
confirming heavy attack execution.
```

The subject identifies one change and the body explains its behavior.

#### Incorrect

```text
cambios varios y arreglos visuales

Arregle el inventario que se desbordaba, cambie el daño de los jefes y
ajuste tres pantallas del menu principal.
```

The commit mixes unrelated work and does not follow the required language or format.

### 10.2 Repository Hygiene and Sensitive Data

Track only source code, documentation, and required project assets in Git (Chacon & Straub, 2014; Microsoft, 2023j):

- Exclude build outputs (`bin/`, `obj/`, `Debug/`, `Release/`), IDE state (`.vs/`, `*.user`, `*.suo`), test outputs (`TestResults/`), and local secret files (`.env`) via `.gitignore`.
- Commit a sanitized `.env.example` template with placeholder values only, manage external libraries via NuGet `PackageReference` entries rather than tracked `.dll` binaries, and immediately revoke/rotate any accidentally committed secret (Section 8.4).

#### Correct

```text
Tracked repository files:
- src/DungeonQuest.Server/Sessions/ArenaSessionService.cs
- .gitignore (ignoring .env, bin/, obj/, .vs/, TestResults/)
- .env.example (GAME_DB_CONNECTION=Server=localhost;Database=DungeonQuest;User Id=PLACEHOLDER;)
```

Only source, configuration templates, and ignore rules are tracked.

#### Incorrect

```text
Tracked repository files:
- src/DungeonQuest.Server/bin/Debug/DungeonQuest.Server.dll
- .vs/DungeonQuest/v17/.suo
- .env (GAME_DB_PASSWORD=ProductionSecret2026!)
```

The tracked files contain build output, editor state, and a real secret.

---

## 11. References

Chacon, S., & Straub, B. (2014). *Pro Git* (2nd ed.). Apress. [https://git-scm.com/book/en/v2](https://git-scm.com/book/en/v2)

Conventional Commits. (2020, February 28). *Conventional Commits 1.0.0*. [https://www.conventionalcommits.org/en/v1.0.0/](https://www.conventionalcommits.org/en/v1.0.0/)

Ecma International. (2023, December). *ECMA-334: C# language specification* (7th ed.). [https://ecma-international.org/publications-and-standards/standards/ecma-334/](https://ecma-international.org/publications-and-standards/standards/ecma-334/)

Esposito, D. (2014, October). Cutting edge: Source code readability tips. *MSDN Magazine, 29*(10). Microsoft. [https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/october/cutting-edge-source-code-readability-tips](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/october/cutting-edge-source-code-readability-tips)

Google. (n.d.). *C# at Google Style Guide*. Google Style Guides. Retrieved August 29, 2026, from [https://google.github.io/styleguide/csharp-style.html](https://google.github.io/styleguide/csharp-style.html)

JetBrains. (2026, March 31). *Invert ‘if’ statement to reduce nesting*. Inspectopedia Documentation. [https://www.jetbrains.com/help/inspectopedia/InvertIf.html](https://www.jetbrains.com/help/inspectopedia/InvertIf.html)

Microsoft. (n.d.-a). *Async semaphores, locks, and reader/writer coordination*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced)

Microsoft. (n.d.-c). *Interlocked class*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked)

Microsoft. (n.d.-d). *Local functions*. Microsoft Learn. Retrieved November 18, 2026, from [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/building-software/local-functions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/building-software/local-functions)

Microsoft. (n.d.-f). *Path.Combine method (System.IO)*. Microsoft Learn. Retrieved October 22, 2026, from [https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine)

Microsoft. (n.d.-g). *SemaphoreSlim class*. Microsoft Learn. Retrieved November 18, 2026, from [https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netframework-4.7.2](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netframework-4.7.2)

Microsoft. (n.d.-b). *The event keyword*. Microsoft Learn. Retrieved November 18, 2026, from [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/event](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/event)

Microsoft. (n.d.-e). *The lock statement: ensure exclusive access to a shared resource*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)

Microsoft. (n.d.-h). *Thread-safe collections*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/](https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/)

Microsoft. (n.d.-i). *WPF globalization and localization overview*. Microsoft Learn. Retrieved November 18, 2026, from [https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-globalization-and-localization-overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-globalization-and-localization-overview)

Microsoft. (2020a, October 14). *No-tracking queries*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking](https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking)

Microsoft. (2020b, October 14). *Querying and finding entities*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/](https://learn.microsoft.com/en-us/ef/ef6/querying/)

Microsoft. (2021a, September 15). *Data contract versioning*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-contract-versioning](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/data-contract-versioning)

Microsoft. (2021b, September 15). *Enumeration types in data contracts*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/enumeration-types-in-data-contracts)

Microsoft. (2021c, September 15). *Error handling in Windows Communication Foundation (WCF)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/error-handling](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/error-handling)

Microsoft. (2021d, December 10). *How to subscribe to and unsubscribe from events (C# Programming Guide)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/how-to-subscribe-to-and-unsubscribe-from-events](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/how-to-subscribe-to-and-unsubscribe-from-events)

Microsoft. (2021e, September 15). *Using data contracts*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/using-data-contracts](https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/using-data-contracts)

Microsoft. (2022a, June 25). *‘using’ directive placement (IDE0065)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0065](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0065)

Microsoft. (2022b, October 20). *‘var’ preferences (IDE0007 and IDE0008)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0007-ide0008](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0007-ide0008)

Microsoft. (2023a, July 25). *Add braces (IDE0011)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0011](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0011)

Microsoft. (2023b, July 25). *Add readonly modifier (IDE0044)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0044](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0044)

Microsoft. (2023c, May 9). *Architectural principles*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

Microsoft. (2023d, April 22). *CA1502: Avoid excessive complexity (code analysis)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1502)

Microsoft. (2023e, July 27). *IDE0019: Use pattern matching to avoid 'as' followed by a 'null' check*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0019](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0019)

Microsoft. (2023f, October 3). *Names of namespaces*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces)

Microsoft. (2023g, October 3). *Names of type members*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members)

Microsoft. (2023h, October 3). *Naming parameters*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-parameters](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-parameters)

Microsoft. (2023i, December 16). *Remove unnecessary using directives (IDE0005)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0005](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0005)

Microsoft. (2023j, March 30). *Securing services*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/securing-services](https://learn.microsoft.com/en-us/dotnet/framework/wcf/securing-services)

Microsoft. (2024, January 18). *Use pattern matching to avoid 'is' check followed by a cast (IDE0020 and IDE0038)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038)

Microsoft. (2025a, August 15). *Asynchronous programming scenarios*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)

Microsoft. (2025b, October 22). *Best practices for exceptions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)

Microsoft. (2025c, January 30). *C# formatting options*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options)

Microsoft. (2025d, February 4). *CA2000: Dispose objects before losing scope (code analysis)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000)

Microsoft. (2025e, October 30). *Code metrics: Cyclomatic complexity*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity)

Microsoft. (2025f, January 18). *Common C# code conventions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

Microsoft. (2025g, September 12). *Documentation comments (C# language specification)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)

Microsoft. (2025h, July 14). *Error message guidelines*. Microsoft Learn. [https://learn.microsoft.com/en-us/windows/win32/debug/error-message-guidelines](https://learn.microsoft.com/en-us/windows/win32/debug/error-message-guidelines)

Microsoft. (2025i, June 17). *How to write good /// docs for .NET API reference*. Microsoft Learn. [https://learn.microsoft.com/en-us/contribute/content/dotnet/api-documentation](https://learn.microsoft.com/en-us/contribute/content/dotnet/api-documentation)

Microsoft. (2025j, March 24). *Introduction to LINQ queries in C#*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries)

Microsoft. (2025k, May 29). *Names of classes, structs, and interfaces*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces)

Microsoft. (2025l, November 19). *Properties (C# Programming Guide)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties)

Microsoft. (2025m, November 17). *Use Task List with code comments and shortcuts*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list](https://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list)

Microsoft. (2026a, January 26). *`readonly` (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly)

Microsoft. (2026b, July 14). *C# identifier naming rules and conventions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)

Microsoft. (2026c, April 2). *CA1711: Identifiers should not have incorrect suffix (code analysis)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711)

Microsoft. (2026d, April 2). *CA2254: Template should be a static expression*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254)

Microsoft. (2026e, April 13). *Extract and inline refactorings*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/ide/reference/refactoring-extract-inline](https://learn.microsoft.com/en-us/visualstudio/ide/reference/refactoring-extract-inline)

Microsoft. (2026f, January 24). *Lambda expressions and anonymous functions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)

Microsoft. (2026g, February 4). *Logging in C# and .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)

Microsoft. (2026h, May 27). *Namespaces and using directives*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces)

Microsoft. (2026i, May 26). *Nullable reference types*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types)

Microsoft. (2026j, June 22). *Recommended XML tags for C# documentation comments*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)

Microsoft. (2026k, April 27). *Testing in .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/](https://learn.microsoft.com/en-us/dotnet/core/testing/)

Microsoft. (2026l, May 26). *Tutorial: Express your design intent with nullable and non-nullable reference types*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types)

Microsoft. (2026m, March 9). *Unit test basics with Test Explorer*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics)

Microsoft. (2026n, January 20). *using statement - ensure the correct use of disposable objects*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)

Microsoft. (2026o, June 24). *Write tests with MSTest*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests)

OWASP Foundation. (n.d.-a). *File upload cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)

OWASP Foundation. (n.d.-b). *Input validation cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html)

OWASP Foundation. (n.d.-c). *Password storage cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

OWASP Foundation. (n.d.-d). *Secrets management cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

The MITRE Corporation. (n.d.). *CWE-20: Improper input validation*. Common Weakness Enumeration. Retrieved October 22, 2026, from [https://cwe.mitre.org/data/definitions/20.html](https://cwe.mitre.org/data/definitions/20.html)

van der Til, J., Costello, M., & Calvarro Nelson, J. (2026, March 10). *Integration tests in ASP.NET Core*. Microsoft Learn. [https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

---

## 12. AI Use Declaration

Artificial Intelligence tools were used as a support resource during the drafting and refinement of this standard for structural organization, wording conciseness, example synthesis, cross-section consistency checks, and reference verification. The authors retain responsibility for approving the rules and checking examples against their supported environment; automated formatting checks do not replace technical review.
