# C# Video Game Development Standard

## Table of Contents

- [[#1. Introduction|1. Introduction]]
  - [[#1.1 Problem Statement|1.1 Problem Statement]]
  - [[#1.2 Purpose|1.2 Purpose]]
- [[#2. Language|2. Language]]
  - [[#2.1 Source Code Language|2.1 Source Code Language]]
  - [[#2.2 User-Facing Language and Localization|2.2 User-Facing Language and Localization]]
- [[#3. Naming Conventions|3. Naming Conventions]]
  - [[#3.1 General Identifier Rules|3.1 General Identifier Rules]]
  - [[#3.2 Namespaces|3.2 Namespaces]]
  - [[#3.3 Classes|3.3 Classes]]
  - [[#3.4 Interfaces|3.4 Interfaces]]
  - [[#3.5 Enumerations|3.5 Enumerations]]
  - [[#3.6 Enumeration Members|3.6 Enumeration Members]]
  - [[#3.7 Fields|3.7 Fields]]
  - [[#3.8 Properties|3.8 Properties]]
  - [[#3.9 Local Variables|3.9 Local Variables]]
  - [[#3.10 Parameters|3.10 Parameters]]
  - [[#3.11 Constants|3.11 Constants]]
  - [[#3.12 Methods|3.12 Methods]]
  - [[#3.13 Events|3.13 Events]]
  - [[#3.14 Test Classes|3.14 Test Classes]]
  - [[#3.15 Test Methods|3.15 Test Methods]]
  - [[#3.16 Prefixes and Suffixes|3.16 Prefixes and Suffixes]]
  - [[#3.17 WPF Controls, Resource Keys, and Client Asset Files|3.17 WPF Controls, Resource Keys, and Client Asset Files]]
- [[#4. Code Style and Formatting|4. Code Style and Formatting]]
  - [[#4.1 General Formatting|4.1 General Formatting]]
  - [[#4.2 Indentation|4.2 Indentation]]
  - [[#4.3 Braces|4.3 Braces]]
  - [[#4.4 Line Length|4.4 Line Length]]
  - [[#4.5 Line Wrapping|4.5 Line Wrapping]]
  - [[#4.6 Horizontal Whitespace|4.6 Horizontal Whitespace]]
  - [[#4.7 Vertical Whitespace|4.7 Vertical Whitespace]]
  - [[#4.8 Using Directives|4.8 Using Directives]]
  - [[#4.9 Namespace Declarations|4.9 Namespace Declarations]]
  - [[#4.10 Early Returns|4.10 Early Returns]]
  - [[#4.11 Source File Organization|4.11 Source File Organization]]
- [[#5. C# Programming Practices|5. C# Programming Practices]]
  - [[#5.1 Access Modifiers|5.1 Access Modifiers]]
  - [[#5.2 Auto-Implemented Properties and Property Accessors|5.2 Auto-Implemented Properties and Property Accessors]]
  - [[#5.3 Immutability and `readonly` Usage|5.3 Immutability and `readonly` Usage]]
  - [[#5.4 Constants and Magic Values|5.4 Constants and Magic Values]]
  - [[#5.5 Method Responsibility|5.5 Method Responsibility]]
  - [[#5.6 Don't Repeat Yourself (DRY)|5.6 Don't Repeat Yourself (DRY)]]
  - [[#5.7 Method Size|5.7 Method Size]]
  - [[#5.8 Parameters|5.8 Parameters]]
  - [[#5.9 Cyclomatic Complexity|5.9 Cyclomatic Complexity]]
  - [[#5.10 Nullable Reference Types|5.10 Nullable Reference Types]]
  - [[#5.11 Null Handling|5.11 Null Handling]]
  - [[#5.12 Pattern Matching|5.12 Pattern Matching]]
  - [[#5.13 LINQ Usage|5.13 LINQ Usage]]
  - [[#5.14 Lambda Expressions|5.14 Lambda Expressions]]
  - [[#5.15 Events and Delegates|5.15 Events and Delegates]]
  - [[#5.16 Resource Disposal|5.16 Resource Disposal]]
  - [[#5.17 Serialization|5.17 Serialization]]
  - [[#5.18 Explicit Local Variable Types|5.18 Explicit Local Variable Types]]
  - [[#5.19 Conditional Operator|5.19 Conditional Operator]]
  - [[#5.20 Thread Synchronization and Concurrency|5.20 Thread Synchronization and Concurrency]]
  - [[#5.21 Asynchronous Mutual Exclusion|5.21 Asynchronous Mutual Exclusion]]
  - [[#5.22 Atomic Operations|5.22 Atomic Operations]]
  - [[#5.23 Concurrent Collections|5.23 Concurrent Collections]]
  - [[#5.24 You Aren't Gonna Need It (YAGNI)|5.24 You Aren't Gonna Need It (YAGNI)]]
- [[#6. Error Handling, Logging, and Diagnostics|6. Error Handling, Logging, and Diagnostics]]
  - [[#6.1 Exception Handling|6.1 Exception Handling]]
  - [[#6.2 Custom Exceptions|6.2 Custom Exceptions]]
  - [[#6.3 Logging|6.3 Logging]]
  - [[#6.4 Log Levels|6.4 Log Levels]]
  - [[#6.5 Log Message Conventions|6.5 Log Message Conventions]]
  - [[#6.6 Assertions|6.6 Assertions]]
  - [[#6.7 Player-Facing Error Messages|6.7 Player-Facing Error Messages]]
  - [[#6.8 Unhandled Exceptions and Diagnostic Reporting|6.8 Unhandled Exceptions and Diagnostic Reporting]]
- [[#7. Comments and Documentation|7. Comments and Documentation]]
  - [[#7.1 General Comment Rules|7.1 General Comment Rules]]
  - [[#7.2 Implementation Comments|7.2 Implementation Comments]]
  - [[#7.3 XML Documentation Comments|7.3 XML Documentation Comments]]
  - [[#7.4 Public API Documentation|7.4 Public API Documentation]]
  - [[#7.5 `TODO` Comments|7.5 `TODO` Comments]]
  - [[#7.6 `FIXME` Comments|7.6 `FIXME` Comments]]
- [[#8. Input Validation and Security|8. Input Validation and Security]]
  - [[#8.1 Trust Boundaries|8.1 Trust Boundaries]]
  - [[#8.2 Input Validation|8.2 Input Validation]]
  - [[#8.3 File System Access|8.3 File System Access]]
  - [[#8.4 Credential and Secret Management|8.4 Credential and Secret Management]]
- [[#9. Testing and Quality Assurance|9. Testing and Quality Assurance]]
  - [[#9.1 Testing Strategy|9.1 Testing Strategy]]
  - [[#9.2 Unit Testing|9.2 Unit Testing]]
  - [[#9.3 Integration Testing|9.3 Integration Testing]]
  - [[#9.4 Arrange-Act-Assert Structure|9.4 Arrange-Act-Assert Structure]]
- [[#10. Version Control and Collaboration|10. Version Control and Collaboration]]
  - [[#10.1 Commit Messages and Atomicity|10.1 Commit Messages and Atomicity]]
  - [[#10.2 Repository Hygiene and Sensitive Data|10.2 Repository Hygiene and Sensitive Data]]
- [[#11. References|11. References]]
- [[#12. AI Use Declaration|12. AI Use Declaration]]

---

## 1. Introduction

This development standard defines the coding rules and practices governing C# video game software development, including desktop presentation (WPF), remote communication contracts (WCF), application logic, persistence (Entity Framework), and automated testing. It prioritizes readability, consistency, maintainability, and defect prevention over condensed syntax (Microsoft, 2025f).

### 1.1 Problem Statement

C# permits expressing identical behavior in multiple syntactic forms that are accepted by the compiler without enforcing consistent naming, formatting, null safety, error handling, or test organization (Microsoft, 2026a). Without verifiable rules, stylistic divergence increases cognitive load, hides defects, and complicates code reviews.

### 1.2 Purpose

This standard establishes clear, verifiable, and consistently applicable rules to:

- Ensure uniform source-code readability and structure across contributors.
- Reduce accidental complexity, duplicated logic, and ambiguous `null` contracts.
- Standardize error handling, structured logging, security boundaries, and automated testing.
- Enable automated enforcement through compiler diagnostics, code analyzers, and `.editorconfig`.

This standard complements the C# language specification and official .NET guidance without replacing them.

**Code Example Scope:** Code examples are intentionally minimal and omit surrounding namespaces, `using` directives, XML documentation, and unrelated members when irrelevant to the rule demonstrated. Every **Correct** example complies with all substantive rules applicable to the behavior shown.

---

## 2. Language

### 2.1 Source Code Language

All handwritten source-code identifiers, comments, XML documentation, and technical log templates must be written in **English** using clear terminology (Microsoft, 2026a). Player-facing text is governed separately by Section 2.2. Automatically generated code is excluded and must retain the generator's original language and structure.

#### Correct

```csharp
int remainingLives = player.RemainingLives;
bool isBossDefeated = false;

// Prevent the player from moving during the cutscene.

// Generated file: preserve the generator output without manual translation.
```

The handwritten names use English while generated output remains untouched.

#### Incorrect

```csharp
int vidasRestantes = player.RemainingLives;
bool jefeDerrotado = false;

// Prevent player movement during la cinemática.

// Manually translated generated code.
```

The handwritten code mixes languages and proposes editing generated output.

### 2.2 User-Facing Language and Localization

All culture-dependent player-facing text must be stored in localization resource dictionaries rather than hardcoded in UI markup or C# logic. Supported cultures (`en-US` and `es-MX`) must define identical, stable English resource keys with matching format placeholders. Image assets containing embedded text must provide localized variants; language-neutral visual assets must not be duplicated per culture.

#### Correct

```csharp
string turnBanner = string.Format(
    CultureInfo.CurrentUICulture,
    Strings.Combat_CurrentTurnFormat,
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

Identifiers must use descriptive domain names that communicate intent without inspecting the implementation. Unclear abbreviations and consecutive underscores (`__`, reserved for compiler-generated names) are prohibited. Single-letter names are permitted only for trivial local loop counters (Microsoft, 2026a).

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

Namespace names must use **PascalCase** separated by periods to form a clear hierarchy starting with the root product name followed by the architectural layer or functional area, such as `Gameplay`, `Combat`, `Contracts`, or `Persistence` (Microsoft, 2023e).

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

Class names must use **PascalCase** nouns or noun phrases that represent a single concept or responsibility, without type prefixes such as `C` (Microsoft, 2025j; Microsoft, 2026a):

- Stateless application coordinators that group cohesive operations for one capability must use `{Capability}ApplicationService`. Do not fragment one capability across `UseCase`, `Handler`, `Reader`, `Editor`, or `Operations` classes unless an independent abstraction is justified.
- Persistence repository implementations must be named `{Aggregate}Repository` after the domain aggregate they manage, without embedding technology names such as `EntityFramework` or `SqlServer`.
- Dedicated `Command`, `Handler`, or `Mapper` classes must encapsulate a distinct, reusable responsibility. Single-boundary data conversion belongs in a private method, and `Command` types must not duplicate existing transport DTOs.

#### Correct

```csharp
public sealed class QuestApplicationService
{
}

public sealed class QuestRepository : IQuestRepository
{
}
```

Each class name identifies its responsibility without naming an implementation technology.

#### Incorrect

```csharp
public sealed class Cquest_operations
{
}

public sealed class EntityFrameworkQuestRepository : IQuestRepository
{
}
```

The names use invalid casing and expose a persistence technology.

### 3.4 Interfaces

Interface names must use **PascalCase** prefixed with `I` and describe a capability, behavior, or contract using an adjective phrase or noun phrase (Microsoft, 2025j; Microsoft, 2026a). The word `Interface` must not appear in the name.

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

Enumeration names must use **PascalCase** singular nouns, except `[Flags]` bitmask enumerations, which must use plural nouns. The suffixes `Enum`, `Flag`, and `Flags` are prohibited (Microsoft, 2025j; Microsoft, 2026a).

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

Enumeration member names must use **PascalCase** and must not repeat the enumeration type name as a prefix (Microsoft, 2025j).

#### Correct

```csharp
public enum AttackType
{
    LightAttack,
    HeavyAttack,
    SpecialAttack
}
```

Each member uses PascalCase without repeating the enumeration name.

#### Incorrect

```csharp
public enum AttackType
{
    attack_light,
    attack_heavy,
    attack_special
}
```

The members use underscores and lowercase prefixes instead of PascalCase.

### 3.7 Fields

Field naming must distinguish instance state, immutable runtime constants, and mutable static state (Microsoft, 2026a):

- Private and internal **instance** fields (both mutable and `readonly`) must use `_camelCase`.
- Immutable `static readonly` fields must use **PascalCase**.
- Mutable `private static` fields must use `s_camelCase`, and `[ThreadStatic]` fields must use `t_camelCase`.
- Mutable `public`, `protected`, or `internal` fields are prohibited.

#### Correct

```csharp
public sealed class SessionTracker
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static int s_activeTrackerCount;
    private readonly ISessionStore _sessionStore;
    private int _retryCount;

    public SessionTracker(ISessionStore sessionStore)
    {
        _sessionStore = sessionStore
            ?? throw new ArgumentNullException(nameof(sessionStore));
    }
}
```

The field names reveal whether state belongs to an instance or the type.

#### Incorrect

```csharp
public sealed class SessionTracker
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    public static int ActiveTrackerCount;
    private readonly ISessionStore SessionStore;
    private int retryCount;

    public SessionTracker(ISessionStore sessionStore)
    {
        SessionStore = sessionStore;
    }
}
```

The fields use inconsistent casing and expose mutable static state publicly.

### 3.8 Properties

Property names must use **PascalCase** nouns, noun phrases, or adjectives. Boolean properties must use affirmative names prefixed with `Is`, `Has`, `Can`, or `Should` when doing so clarifies state (Microsoft, 2023f).

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

Local variable names must use **camelCase** and clearly identify the stored value without arbitrary abbreviations or underscores (Microsoft, 2026a).

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

Method and constructor parameters must use **camelCase** and describe the semantic role of the argument rather than repeating only its data type (Microsoft, 2023g). Rules governing `out` and `ref` parameters are defined in Section 5.8.

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

Compile-time `const` identifiers must use **PascalCase** regardless of accessibility; `UPPER_SNAKE_CASE` is prohibited (Microsoft, 2026a).

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

Method names must use **PascalCase** and start with a verb or verb phrase describing the action performed rather than only the data returned or modified (Microsoft, 2023f; Microsoft, 2026a).

#### Correct

```csharp
public int CalculateDamage(int baseDamage)
{
    return baseDamage;
}
```

The method begins with a PascalCase verb that describes its action.

#### Incorrect

```csharp
public int damage_calculation(int baseDamage)
{
    return baseDamage;
}
```

The method uses invalid casing and names a result instead of an action.

### 3.13 Events

Event names must use **PascalCase** verb phrases. Use verb tense (such as `Closing` and `Closed`) to distinguish pre-action and post-action notifications; prefixes `Before` and `After` are prohibited (Microsoft, 2023f).

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

Test classes and test support types must use **PascalCase** suffixes that identify their verification scope (Microsoft, 2026o; van der Til et al., 2026):

- `<TypeUnderTest>Tests` for unit tests targeting a single production class.
- `<Feature>IntegrationTests` for tests verifying collaboration across real components or infrastructure.
- `<Feature>SerializationTests` for WCF data-contract round-trip and compatibility tests.
- `Stub`, `Fake`, or `Context` suffixes for reusable test doubles and fixture support classes.

#### Correct

```csharp
[TestClass]
public sealed class PlayerHealthTests
{
}

[TestClass]
public sealed class InventoryPersistenceIntegrationTests
{
}

internal sealed class InMemoryInventoryRepositoryFake : IInventoryRepository
{
}
```

Each type name states whether it verifies one class, an integration, or supports a test.

#### Incorrect

```csharp
[TestClass]
public sealed class TestPlayerStuff
{
}

internal sealed class MockHelper : IInventoryRepository
{
}
```

The names do not identify the verified scope or the test double's behavior.

### 3.15 Test Methods

Test method names must contain three or four underscore-separated **PascalCase** segments: `Method_Scenario_ExpectedBehavior` or `Method_Scenario_ExpectedBehavior_Flow` (Reese, 2025).

#### Correct

```csharp
public void TakeDamage_HealthAboveZero_DecreasesHealth()
{
}

public void TakeDamage_NegativeAmount_ThrowsArgumentOutOfRange_Failed()
{
}
```

Each test name identifies the method, scenario, and expected behavior.

#### Incorrect

```csharp
public void TestDamage()
{
}
```

The name does not reveal the tested scenario or expected result.

### 3.16 Prefixes and Suffixes

Prefixes and suffixes are restricted to meanings established by .NET guidelines or this standard (Microsoft, 2025b; Microsoft, 2025j; Microsoft, 2026a, 2026b):

| Element | Required Form |
| --- | --- |
| Interface | Prefix `I` (`IDamageable`, `IQuestRepository`). |
| Descriptive generic type parameter | Prefix `T` (`TItem`). |
| Private/internal instance field | Prefix `_` (`_currentHealth`). |
| Mutable private static / thread-static field | Prefix `s_` / `t_` (`s_instanceCount`, `t_requestId`). |
| Application service / repository | Suffix `ApplicationService` / `Repository`. |
| Asynchronous method | Use `Async` when synchronous and asynchronous alternatives coexist. |
| Custom exception | Suffix `Exception` (`InventoryFullException`). |
| Custom event payload | Suffix `EventArgs` (`ItemEquippedEventArgs`). |
| Custom delegates | Suffix `EventHandler` or `Callback`. |

- Class prefixes such as `C` and suffixes such as `Enum`, `Delegate`, `Impl`, `Ex`, and `New` are prohibited (`CA1711`).
- Methods returning `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` should use the
  `Async` suffix when synchronous and asynchronous alternatives coexist. A cohesive
  abstraction that exposes only asynchronous operations and has no synchronous alternative
  may omit the suffix. The selected convention must remain consistent throughout that
  abstraction. Event handlers, framework overrides, generated members, and external contract
  signatures retain their required names. Synchronous methods must never end with `Async` and
  must not wrap asynchronous calls through blocking operations (`.Result`, `.Wait()`, or
  `.GetAwaiter().GetResult()`) (Microsoft, 2025b).

#### Correct

```csharp
public interface IMatchChannel
{
    Task SendMessage(string content);

    Task CloseChannel();
}
```

The suffix is omitted consistently because the abstraction exposes only asynchronous
operations.

#### Incorrect

```csharp
public interface IMatchChannel
{
    Task SendMessageAsync(string content);

    Task CloseChannel();
}
```

The abstraction mixes two naming conventions without offering synchronous alternatives.

### 3.17 WPF Controls, Resource Keys, and Client Asset Files

Named WPF controls (`x:Name`) must use **camelCase** combining semantic purpose and control type (such as `languageComboBox`, `usernameTextBox`, `confirmSelectionButton`); Hungarian prefixes (`btn`, `txt`, `lbl`, `img`) are prohibited. View classes and XAML styles must use **PascalCase** (`QuestJournalPage`, `PrimaryActionButtonStyle`).

Localization resource keys must use `<Feature>_<Purpose>` in English **PascalCase** separated by one underscore:

- Keys must describe semantic purpose rather than literal wording, avoid spaces/hyphens/accents/abbreviated control prefixes, and exist identically across all supported `.resx` cultures.
- Keys must not be renamed when only display wording changes, and formatted strings containing `{0}` placeholders must end with `Format` and preserve placeholder compatibility across cultures.

| Resource Purpose | Pattern | Example |
| --- | --- | --- |
| Window, page, or section title | `<Feature>_Title` | `QuestJournal_Title` |
| Field or control label | `<Feature>_<Meaning>Label` | `Settings_MasterVolumeLabel` |
| Button text | `<Feature>_<Action>Button` | `Inventory_DropItemButton` |
| Validation or status message | `<Feature>_<Meaning>Message` | `SignIn_InvalidCredentialsMessage` |
| Formatted runtime string | `<Feature>_<Meaning>Format` | `Combat_CurrentTurnFormat` |
| Input hint or placeholder | `<Feature>_<Meaning>Placeholder` | `GuildSearch_NamePlaceholder` |
| Control tooltip | `<Feature>_<Meaning>Tooltip` | `Hud_LeaveDungeonTooltip` |

Client asset files must use descriptive English **PascalCase** names. Store them under a
folder that identifies the media kind, such as `Assets/Images`. Do not use spaces, hyphens,
sequence numbers, or generic names such as `image` and `icon1`.

#### Correct

```text
QuestJournal_Title
Combat_CurrentTurnFormat
DefaultProfilePicture.png
BoardMask.png
```

The resource keys and asset files use stable English names that describe their purpose.

#### Incorrect

```text
tituloMisiones
Combat_ItIsNowTheTurnOfPlayer
default profile picture.png
image1.png
```

The names mix languages, encode display wording, or fail to identify the asset purpose.

---

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

All braces must follow the **Allman style**, placing `{` and `}` on dedicated lines aligned with their owning declaration or control statement (Microsoft, 2023a; Microsoft, 2025f):

- Every `if`, `else`, `for`, `foreach`, `while`, `do`, and `lock` statement must use braces even when its body contains a single statement (`IDE0011`).
- Handwritten methods, constructors, properties, indexers, and custom accessors must use explicit block bodies (`{ ... }`). Expression-bodied members (`=>`) are prohibited in handwritten code; inline lambda expressions passed as delegates remain governed by Section 5.14.

#### Correct

```csharp
public int CalculateEffectiveArmor(int baseArmor, bool isShieldRaised)
{
    if (isShieldRaised)
    {
        return baseArmor + ShieldArmorBonus;
    }

    return baseArmor;
}
```

The method and conditional use Allman braces with explicit block bodies.

#### Incorrect

```csharp
public int DefaultArmor => DefaultArmorValue;

public int CalculateEffectiveArmor(int baseArmor, bool isShieldRaised)
{
    if (isShieldRaised) {
        return baseArmor + ShieldArmorBonus;
    }
    else
        return baseArmor;
}
```

The member uses an expression body and the conditional omits required brace placement.

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

Apply horizontal spacing consistently (Microsoft, 2025d):

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

Place `using` directives at the top of the file outside the namespace (`IDE0065`), sorted alphabetically with `System.*` namespaces first, and remove unused imports (`IDE0005`) (Microsoft, 2022a; Microsoft, 2023h, 2023i; Microsoft, 2025f). Type alias directives (`using Alias = Namespace.Type;`) are permitted only at the bottom of the `using` block to disambiguate colliding type names across layers or libraries, never as arbitrary shorthand (Microsoft, 2026h).

#### Correct

```csharp
using System;
using System.Collections.Generic;

using GameProject.Gameplay.Items;
using GameProject.UI.Hud;

using ContractItemDto = GameProject.Contracts.Inventory.InventoryItemDto;

namespace GameProject.Client.Inventory;
```

The imports are ordered and the alias resolves a real type collision.

#### Incorrect

```csharp
namespace GameProject.Client.Inventory;

using GameProject.UI.Hud;
using System;
using System.IO;
using ListAlias = System.Collections.Generic.List<int>;
```

The imports appear after the namespace and the alias only shortens a type name.

### 4.9 Namespace Declarations

Every source file must declare an explicit namespace matching its logical module. Prefer **file-scoped namespaces** (`namespace GameProject.Gameplay.Combat;`) unless a file legitimately requires multiple namespaces (Microsoft, 2026h).

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

### 4.10 Early Returns

Check guard conditions, invalid inputs, and legitimate no-operation states at the beginning of a method and return early so the primary execution path remains at base indentation (Ecma International, 2023; JetBrains, 2026):

- Guard clauses must always use Allman braces (Section 4.3).
- Early returns must not be placed mid-operation where they bypass mandatory state updates, persistence, or cleanup; use `using` or `finally` when deterministic cleanup is required.
- Early returns reduce nesting but still contribute to cyclomatic complexity (Section 5.9).

#### Correct

```csharp
public void TryPerformAttack(Player player, Enemy target)
{
    if (!player.IsAlive || !player.HasWeapon)
    {
        return;
    }

    if (!target.IsAlive || !player.CanReach(target))
    {
        return;
    }

    PerformAttack(player, target);
}
```

The guard clauses leave the valid operation at the base indentation level.

#### Incorrect

```csharp
public void CompleteDungeonFloor(Player player)
{
    if (player.IsAlive)
    {
        SaveCheckpoint(player);

        if (!player.HasBonusChestKey)
        {
            return;
        }

        GrantBonusTreasure(player);
        MarkFloorCompleted(player);
    }
}
```

The return occurs after persistence and skips the required completion update.

### 4.11 Source File Organization

Each behavioral class, domain entity, application service, repository, contract interface, or custom `Exception` class must reside in its own `.cs` file whose filename matches the type identifier (`<TypeName>.cs`) (Microsoft, 2025j):

- A `{Capability}Models.cs` container file is permitted solely to group small, passive input/output data models and status enumerations that are cohesive and exclusive to one capability.
- `*Models.cs` files must never contain `Exception` types, interfaces, services, or domain entities with business rules or state initialization logic.

#### Correct

```csharp
// File: ArenaSession.cs
public sealed class ArenaSession
{
    public Guid SessionId { get; }

    public ArenaSession(Guid sessionId)
    {
        SessionId = sessionId;
    }
}

// File: ArenaModels.cs (passive data models and status enums only)
public enum ArenaJoinStatus
{
    Joined,
    ArenaFull,
    ArenaClosed
}

public sealed class ArenaParticipantSummary
{
    public long AccountId { get; init; }

    public string DisplayName { get; init; } = string.Empty;
}
```

The behavioral type has its own file while the container holds only passive models.

#### Incorrect

```csharp
// File: ArenaModels.cs (mixes domain entity logic, exception, and DTOs)
public sealed class ArenaSession
{
    public void InitializeSpawnGrid(int width, int height)
    {
        // Domain initialization rules embedded in a Models container file.
    }
}

public sealed class ArenaCapacityExceededException : Exception
{
}
```

The container mixes domain behavior with an exception type.

---

## 5. C# Programming Practices

### 5.1 Access Modifiers

Declare access modifiers explicitly on every type and member where C# permits one, selecting the **most restrictive visibility** (`private`, `internal`, `protected`, `public`, `private protected`, `protected internal`) required for valid collaboration (Ecma International, 2023). Do not expose mutable fields publicly or widen visibility merely to simplify testing.

#### Correct

```csharp
public sealed class PlayerHealth
{
    private const int InitialHealth = 100;
    private const int MinimumHealth = 0;

    public int CurrentHealth { get; private set; } = InitialHealth;

    public void TakeDamage(int damage)
    {
        int clampedDamage = NormalizeDamage(damage);
        CurrentHealth = Math.Max(CurrentHealth - clampedDamage, MinimumHealth);
    }

    private int NormalizeDamage(int damage)
    {
        return Math.Max(damage, 0);
    }
}
```

Every member declares the narrowest visibility needed by its callers.

#### Incorrect

```csharp
class PlayerHealth
{
    public int CurrentHealth { get; set; } = MaximumHealth;

    int NormalizeDamage(int damage)
    {
        return Math.Max(damage, 0);
    }
}
```

The implicit access modifiers and public setter expose more access than required.

### 5.2 Auto-Implemented Properties and Property Accessors

Use auto-implemented properties whenever custom accessor logic is unnecessary, exposing only required accessors (`{ get; }`, `{ get; init; }`, `{ get; private set; }`, or `{ get; set; }`). Declare an explicit backing field only when validation, clamping, or state coordination is required (Microsoft, 2025k).

#### Correct

```csharp
public sealed class AudioSettings
{
    private const int MinimumVolume = 0;
    private const int MaximumVolume = 100;
    private int _masterVolume;

    public string OutputDeviceName { get; init; } = string.Empty;

    public int MasterVolume
    {
        get
        {
            return _masterVolume;
        }
        set
        {
            _masterVolume = Math.Clamp(value, MinimumVolume, MaximumVolume);
        }
    }
}
```

The backing field exists because the setter enforces a valid range.

#### Incorrect

```csharp
public sealed class AudioSettings
{
    private string _outputDeviceName = string.Empty;

    public string OutputDeviceName
    {
        get
        {
            return _outputDeviceName;
        }
        set
        {
            _outputDeviceName = value;
        }
    }
}
```

The backing field repeats behavior already provided by an auto-implemented property.

### 5.3 Immutability and `readonly` Usage

Model configuration data, definitions, identifiers, and injected dependencies as immutable after construction using `{ get; }` or `{ get; init; }` properties and `readonly` fields (`IDE0044`) (Microsoft, 2023b; Microsoft, 2026e, 2026l). Note that `readonly` on a reference-type field prevents reassigning the reference without making the referenced object itself immutable (Microsoft, 2026j). Keep gameplay state mutable where mutation reflects domain behavior.

#### Correct

```csharp
internal sealed class EnemySpawner
{
    private readonly IEnemyFactory _enemyFactory;

    internal EnemySpawner(IEnemyFactory enemyFactory)
    {
        _enemyFactory = enemyFactory
            ?? throw new ArgumentNullException(nameof(enemyFactory));
    }

    internal Enemy SpawnEnemy(EnemyDefinition definition)
    {
        return _enemyFactory.CreateEnemy(definition);
    }
}
```

The injected dependency cannot be reassigned after construction.

#### Incorrect

```csharp
internal sealed class EnemySpawner
{
    private IEnemyFactory _enemyFactory;

    internal EnemySpawner(IEnemyFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }
}
```

The dependency remains mutable even though reassignment is not part of the behavior.

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

Apply the **Single Responsibility Principle (SRP)** and **Keep It Simple (KISS)**: each class must have one cohesive reason to change, and the simplest design that clearly satisfies the required behavior must be preferred. Do not add pass-through types, layers, or indirection without a distinct responsibility (Microsoft, 2023c).

#### Correct

```csharp
public sealed class PlayerHealth
{
    public int CurrentHealth { get; private set; }

    public PlayerHealth(int initialHealth)
    {
        CurrentHealth = initialHealth;
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(CurrentHealth - damage, 0);
    }
}
```

The class changes only the health state that it owns.

#### Incorrect

```csharp
public sealed class PlayerHealth
{
    public int CurrentHealth { get; private set; }

    public void TakeDamageAndPersist(int damage)
    {
        CurrentHealth = Math.Max(CurrentHealth - damage, 0);
        SaveHealthToDatabase();
        PlayDamageSoundEffect();
    }

    private void SaveHealthToDatabase()
    {
    }

    private void PlayDamageSoundEffect()
    {
    }
}
```

The method combines domain state with persistence and presentation effects.

### 5.6 Don't Repeat Yourself (DRY)

Apply the **Don't Repeat Yourself (DRY)** principle by keeping each rule or calculation in
one authoritative location (Microsoft, 2023c; Microsoft, 2026d). Reuse that implementation
when the meaning is identical. Similar-looking code may remain separate when it represents
different domain concepts.

#### Correct

```csharp
private const int MinimumDamage = 1;

public int CalculatePlayerAttack(int baseDamage, int enemyArmor)
{
    return CalculateDamageAfterArmor(baseDamage, enemyArmor);
}

public int CalculateEnemyAttack(int baseDamage, int playerArmor)
{
    return CalculateDamageAfterArmor(baseDamage, playerArmor);
}

private int CalculateDamageAfterArmor(int baseDamage, int armor)
{
    return Math.Max(baseDamage - armor, MinimumDamage);
}
```

One method owns the shared armor calculation.

#### Incorrect

```csharp
public int CalculatePlayerAttack(int baseDamage, int enemyArmor)
{
    return Math.Max(baseDamage - enemyArmor, 1);
}

public int CalculateEnemyAttack(int baseDamage, int playerArmor)
{
    return Math.Max(baseDamage - playerArmor, 1);
}
```

The same rule and unexplained limit are duplicated in separate methods.

### 5.7 Method Size

Method length is a review signal, not an automatic reason to split code. Extract a helper only when it receives a clear name and owns a cohesive responsibility; keep a longer linear method when splitting it would hide the execution flow (Esposito, 2014).

#### Correct

```csharp
public void ResolveEnemyDefeat(Enemy enemy)
{
    AwardExperience(enemy);
    TrySpawnLoot(enemy);
    _enemyRegistry.Remove(enemy);
}

private void AwardExperience(Enemy enemy)
{
    _playerProgress.AddExperience(enemy.ExperienceReward);
}

private void TrySpawnLoot(Enemy enemy)
{
    if (enemy.LootTable is not null)
    {
        _lootSpawner.Spawn(enemy.LootTable, enemy.Position);
    }
}
```

Each extracted method has a clear responsibility and name.

#### Incorrect

```csharp
public void ResolveEnemyDefeat(Enemy enemy)
{
    // Combines experience scaling, loot drops, boss music transitions,
    // door unlocking, achievements, checkpoint saving, UI refreshes,
    // statistics tracking, and logging in a single 45-line method body
    // instead of delegating to focused helper methods.
}
```

The method description shows unrelated responsibilities with no readable separation.

### 5.8 Parameters

Keep parameter lists short enough to read at the call site. Group values only when they form a cohesive concept; do not create artificial parameter objects to satisfy a numeric limit. Project-defined methods must not declare `out` parameters. Standard .NET `Try` methods may be called when they are the clearest non-throwing or atomic API. Use `ref` only when required by an atomic `Interlocked` operation, and never expose `out` or `ref` through a WCF operation (Microsoft, 2021e; Microsoft, 2025n).

#### Correct

```csharp
public SpawnResult SpawnEnemy(EnemySpawnRequest request)
{
    Guid spawnId;
    if (!Guid.TryParse(request.SpawnId, out spawnId))
    {
        return SpawnResult.InvalidIdentifier;
    }

    return Spawn(request, spawnId);
}
```

The cohesive request keeps the signature small while the framework parse method avoids exceptions.

#### Incorrect

```csharp
public bool TrySpawnEnemy(
    string spawnId,
    out Enemy enemy,
    out string errorMessage)
{
    enemy = new Enemy();
    errorMessage = string.Empty;
    return true;
}
```

The project-defined method exposes multiple output values through `out` parameters.

### 5.9 Cyclomatic Complexity

Each method must maintain a cyclomatic complexity of **10 or lower** (`CA1502`) (Microsoft, 2023d; Microsoft, 2025e). Decompose methods with excessive independent branches into focused domain methods.

#### Correct

```csharp
public EnemyAction DecideAction(Enemy enemy)
{
    if (!enemy.IsAlive)
    {
        return EnemyAction.None;
    }

    if (enemy.IsStunned)
    {
        return EnemyAction.Wait;
    }

    return DecideCombatAction(enemy);
}
```

The guard conditions keep the decision flow below the configured complexity limit.

#### Incorrect

```csharp
public int CalculatePriority(Enemy e)
{
    int p = 0;
    if (e.IsAlive) { p++; }
    if (e.IsVisible) { p++; }
    if (e.HasTarget) { p++; }
    if (e.CanSeePlayer) { p++; }
    if (e.IsInAttackRange) { p++; }
    if (e.HasEnoughStamina) { p++; }
    if (e.IsBoss) { p++; }
    if (e.IsEnraged) { p++; }
    if (e.IsLowHealth) { p++; }
    if (e.CanUseSpecialAttack) { p++; }
    return p;
}
```

The independent branches make the method difficult to reason about and test.

### 5.10 Nullable Reference Types

Enable nullable reference type analysis (`<Nullable>enable</Nullable>`) across all projects. Declare reference types as non-nullable unless absence is a valid, intentional part of the contract (`?`). Do not apply the null-forgiving operator (`!`) to silence compiler warnings without an established runtime invariant (Microsoft, 2026i).

#### Correct

```csharp
public sealed class PlayerProfile
{
    public string DisplayName { get; }

    public string? ClanTag { get; }

    public PlayerProfile(string displayName, string? clanTag)
    {
        DisplayName = displayName
            ?? throw new ArgumentNullException(nameof(displayName));
        ClanTag = clanTag;
    }
}
```

Only the genuinely optional clan tag is declared nullable.

#### Incorrect

```csharp
public sealed class PlayerProfile
{
    public string DisplayName { get; set; } = null!;
}
```

The null-forgiving operator hides an uninitialized required property.

### 5.11 Null Handling

Methods must not return `null` as an ambiguous sentinel value; return empty collections or explicit result objects instead (Ecma International, 2023; Microsoft, 2026i, 2026n):

- **Null-coalescing (`??` and `?? throw`):** Use `??` only to supply a valid domain fallback for an optional value, and use `parameter ?? throw new ArgumentNullException(nameof(parameter))` to reject missing required arguments or constructor dependencies immediately. Never use `??` to substitute a silent default that masks invalid state.
- **Null-conditional (`?.`):** Single-level `?.` is permitted on legitimately optional references when skipping the call is safe (such as `NavigationService?.Navigate(page)` or `stream?.Dispose()`). Deep chains (`a?.b?.c`) and using `?.` to bypass required parameter validation are prohibited.

#### Correct

```csharp
public sealed class GuildRosterService
{
    private readonly IGuildRepository _guildRepository;

    public GuildRosterService(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository
            ?? throw new ArgumentNullException(nameof(guildRepository));
    }

    public IReadOnlyList<GuildMember> GetOnlineMembers(IReadOnlyList<GuildMember>? members)
    {
        if (members is null || members.Count == 0)
        {
            return Array.Empty<GuildMember>();
        }

        return members.Where(member => member.IsOnline).ToArray();
    }

    public void ClosePreviewStream(Stream? previewStream)
    {
        previewStream?.Dispose();
    }
}
```

Required state is rejected immediately while optional values have explicit behavior.

#### Incorrect

```csharp
public sealed class GuildRosterService
{
    private readonly IGuildRepository? _guildRepository;

    public GuildRosterService(IGuildRepository? guildRepository)
    {
        _guildRepository = guildRepository ?? null;
    }

    public IReadOnlyList<GuildMember>? GetOnlineMembers(SessionContext? context)
    {
        bool allowLookup = context?.Account?.Guild?.Permissions?.CanViewRoster == true;
        if (!allowLookup)
        {
            return null;
        }

        return Array.Empty<GuildMember>();
    }
}
```

The code masks a missing dependency and returns `null` with ambiguous meaning.

### 5.12 Pattern Matching

Prefer declaration patterns (`value is Type typedValue`), negative guard patterns (`value is not Type typedValue`), and explicit null patterns (`is null`, `is not null`) over `as` followed by a null check (`IDE0019`) or `is` followed by an explicit cast (`IDE0020`, `IDE0038`) (Ecma International, 2023; Microsoft, 2023k; Microsoft, 2024):

- Relational and logical combinator patterns (`and`, `or`) and flat `switch` expressions are permitted when each arm maps directly to a single readable value without nested switches or side effects.
- Extract multi-condition domain rules into named methods rather than nesting property patterns, and never use the `var` pattern (Section 5.18).

#### Correct: Guard Pattern and Flat `switch` Expression

```csharp
private void OnInventorySlotClick(object sender, RoutedEventArgs eventArgs)
{
    if (sender is not Button slotButton
        || slotButton.DataContext is not InventorySlotItem slotItem)
    {
        return;
    }

    InspectSlotItem(slotItem);
}

private static int GetRaritySortWeight(ItemRarity rarity)
{
    const int UnknownWeight = 0;
    const int CommonWeight = 1;
    const int RareWeight = 2;
    const int LegendaryWeight = 3;

    return rarity switch
    {
        ItemRarity.Common or ItemRarity.Uncommon => CommonWeight,
        ItemRarity.Rare => RareWeight,
        ItemRarity.Legendary => LegendaryWeight,
        _ => UnknownWeight
    };
}
```

The patterns remove casts and the flat switch maps each value directly.

#### Incorrect: Redundant Casts and Nested `switch` Expressions

```csharp
private void OnInventorySlotClick(object sender, RoutedEventArgs eventArgs)
{
    Button? slotButton = sender as Button;
    if (slotButton != null && slotButton.DataContext is InventorySlotItem)
    {
        InventorySlotItem slotItem = (InventorySlotItem)slotButton.DataContext;
        InspectSlotItem(slotItem);
    }
}

private static int GetItemWeight(InventorySlotItem item)
{
    const int HighLevelThreshold = 50;
    const int HighLevelLegendaryWeight = 10;
    const int LegendaryWeight = 5;
    const int DefaultWeight = 1;

    return item.Rarity switch
    {
        ItemRarity.Legendary => item.Level switch
        {
            > HighLevelThreshold => HighLevelLegendaryWeight,
            _ => LegendaryWeight
        },
        _ => DefaultWeight
    };
}
```

The code repeats type checks and nests a second decision inside the switch.

### 5.13 LINQ Usage

Use LINQ method syntax (`Where`, `Select`, `OrderBy`, `Any`, `ToArray`, `ToList`) for side-effect-free collection queries when clearer than manual loops (Microsoft, 2025m):

- Materialize (`ToArray()` or `ToList()`) before exiting a `lock` block so deferred execution cannot enumerate shared mutable state outside synchronization.
- In Entity Framework queries, apply filtering and projection before materialization and use `AsNoTracking()` for read-only queries (Microsoft, 2020a, 2020b).
- Declare explicit result types rather than `var` (Section 5.18).

#### Correct: Synchronized Snapshot and Read-Only Database Projection

```csharp
internal IReadOnlyList<GuildInvite> GetPendingInvites(string recipientName)
{
    GuildInvite[] snapshot;
    lock (_syncRoot)
    {
        snapshot = _invites
            .Where(invite => invite.State == GuildInviteState.Pending)
            .Where(invite => string.Equals(
                invite.RecipientName,
                recipientName,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(invite => invite.CreatedAtUtc)
            .ToArray();
    }

    return snapshot;
}

private static Task<List<PlayerScoreSummary>> LoadActiveScoresAsync(
    GameDbContext context)
{
    return context.PlayerAccounts
        .AsNoTracking()
        .Where(account => account.IsActive)
        .Select(account => new PlayerScoreSummary
        {
            AccountId = account.AccountId,
            DisplayName = account.DisplayName
        })
        .ToListAsync();
}
```

The shared collection is materialized under lock and the database query filters before loading.

#### Incorrect: Deferred Lock Escape and Premature Table Materialization

```csharp
internal IEnumerable<GuildInvite> GetPendingInvites(string recipientName)
{
    lock (_syncRoot)
    {
        return _invites.Where(invite => invite.RecipientName == recipientName);
    }
}

private static async Task<List<PlayerScoreSummary>> LoadActiveScoresAsync(
    GameDbContext context)
{
    List<PlayerAccount> allAccounts = await context.PlayerAccounts.ToListAsync();
    return allAccounts
        .Where(account => account.IsActive)
        .Select(account => new PlayerScoreSummary
        {
            AccountId = account.AccountId,
            DisplayName = account.DisplayName
        })
        .ToList();
}
```

Deferred enumeration escapes the lock and the database table is loaded before filtering.

### 5.14 Lambda Expressions

Lambda expressions are permitted for short, side-effect-free predicates, projections, and inline callbacks containing one expression or at most two simple statements (Google, n.d.; Microsoft, 2025f, 2026f). Extract reused logic or branching into a named method.

#### Correct

```csharp
IEnumerable<Enemy> activeEnemies = enemies.Where(enemy => enemy.IsAlive);
```

The lambda is a short predicate without side effects.

#### Incorrect

```csharp
IEnumerable<Enemy> targetableEnemies = enemies.Where(enemy =>
{
    UpdateDetectionState(enemy);
    if (!enemy.IsAlive || enemy.IsStunned)
    {
        return false;
    }

    RegisterPotentialTarget(enemy);
    return enemy.IsInAttackRange;
});
```

The lambda mutates state and contains branching that belongs in a named method.

### 5.15 Events and Delegates

Use `event EventHandler` or `event EventHandler<TEventArgs>` (where `TEventArgs` derives from `EventArgs`) for publisher-subscriber notifications, raising events via `EventName?.Invoke(this, args)` (Microsoft, 2021d; Microsoft, 2026p):

- Use `Action<T>` or `Func<T, TResult>` delegates instead of events when a callback or return value is required.
- Subscribe with a named handler method whenever the subscriber must later unsubscribe (`-=`) from a longer-lived publisher.

#### Correct

```csharp
public sealed class ItemSlotSelectedEventArgs : EventArgs
{
    public int SlotIndex { get; }

    public ItemSlotSelectedEventArgs(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}

public sealed class InventoryGridControl
{
    public event EventHandler<ItemSlotSelectedEventArgs>? SlotSelected;

    public void SelectSlot(int slotIndex)
    {
        SlotSelected?.Invoke(this, new ItemSlotSelectedEventArgs(slotIndex));
    }
}
```

The event follows the .NET event pattern and carries a typed payload.

#### Incorrect

```csharp
public sealed class InventoryGridControl
{
    public Action<int>? SlotSelected;
    public event Func<string, string>? TooltipTextRequested;
}
```

The public delegate field can be overwritten and the event incorrectly expects a return value.

### 5.16 Resource Disposal

Dispose every locally owned `IDisposable` instance deterministically using a `using` statement or `using` declaration (`CA2000`) so early returns and exceptions cannot leak resources (Microsoft, 2020c; Microsoft, 2025n; Microsoft, 2026q).

#### Correct

```csharp
public async Task<IReadOnlyList<QuestRecord>> GetCompletedQuestsAsync(long accountId)
{
    using (GameDbContext context = new GameDbContext())
    {
        return await context.QuestRecords
            .AsNoTracking()
            .Where(record => record.AccountId == accountId && record.IsCompleted)
            .ToListAsync();
    }
}
```

The context is disposed even when the asynchronous query throws.

#### Incorrect

```csharp
public async Task<IReadOnlyList<QuestRecord>> GetCompletedQuestsAsync(long accountId)
{
    GameDbContext context = new GameDbContext();
    List<QuestRecord> records = await context.QuestRecords
        .Where(record => record.AccountId == accountId && record.IsCompleted)
        .ToListAsync();

    context.Dispose();
    return records;
}
```

An exception before the explicit disposal leaks the context.

### 5.17 Serialization

Define DTOs only for an actual transport boundary. Do not introduce equivalent DTOs between internal layers that can share an existing model. Service-boundary transport DTOs must use explicit `DataContractSerializer` attributes rather than exposing domain entities, persistence models, or presentation types (Microsoft, 2021a, 2021b, 2021e):

- Decorate classes with `[DataContract(Namespace = ContractNamespaces.Data)]` and serialized properties with `[DataMember(Order = n)]`. Never renumber existing `Order` values; append new optional members with higher `Order` numbers, and avoid combining `IsRequired = true` with `EmitDefaultValue = false`.
- Decorate contract enumerations with `[DataContract]` and every serialized value with `[EnumMember]`.
- Treat deserialized payloads as untrusted input requiring full domain validation (Section 8.2), and verify contract changes with round-trip serialization tests.

#### Correct: Explicit Data Contract and Round-Trip Helper

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public enum SaveSyncStatusCode
{
    [EnumMember]
    Synced,

    [EnumMember]
    ConflictDetected
}

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class SaveSlotUploadRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public int SlotNumber { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public string ChecksumHex { get; set; } = string.Empty;

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? OptionalNotes { get; set; }
}

private static TContract RoundTripContract<TContract>(TContract contract)
{
    DataContractSerializer serializer = new DataContractSerializer(typeof(TContract));
    using MemoryStream stream = new MemoryStream();

    serializer.WriteObject(stream, contract);
    stream.Position = 0;

    object? deserialized = serializer.ReadObject(stream);
    Assert.IsNotNull(deserialized);
    return (TContract)deserialized;
}
```

The attributes define stable wire members and the helper verifies serialization compatibility.

#### Incorrect: Implicit Contract and Renumbered Member Order

```csharp
// Implicit contract without [DataContract]/[DataMember] governance, or breaking
// wire compatibility by swapping Order = 1 and Order = 2 on an existing DTO.
public sealed class SaveSlotUploadRequest
{
    public string ChecksumHex { get; set; } = string.Empty;

    public int SlotNumber { get; set; }
}
```

The implicit contract provides no stable member order for future compatibility.

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

Protect shared mutable state with `lock` on a dedicated `private readonly object`. The `object` is a private reference used only as the identity of the monitor acquired by the `lock` statement. It does not store business data. This standard uses `object` because `System.Threading.Lock` requires newer target frameworks and language versions. Keep the block short and perform external work after releasing it. Never lock on `this`, a type, or a string, and never use `await` inside `lock` (Microsoft, n.d.-b).

#### Correct

```csharp
public sealed class ActiveLobbyRegistry
{
    private readonly object _syncRoot = new object();
    private readonly HashSet<Guid> _activeLobbyIds = new HashSet<Guid>();

    public bool RegisterLobby(Guid lobbyId)
    {
        bool wasAdded;
        lock (_syncRoot)
        {
            wasAdded = _activeLobbyIds.Add(lobbyId);
        }

        return wasAdded;
    }
}
```

The dedicated object protects one collection and the method returns after releasing the lock.

#### Incorrect

```csharp
public sealed class ActiveLobbyRegistry
{
    private readonly HashSet<Guid> _activeLobbyIds = new HashSet<Guid>();

    public void RegisterLobby(Guid lobbyId)
    {
        lock (this)
        {
            _activeLobbyIds.Add(lobbyId);
            _remoteService.NotifyLobbyCreated(lobbyId);
        }
    }
}
```

The public instance can be locked by other code, and the remote call extends the critical section.

### 5.21 Asynchronous Mutual Exclusion

Use `SemaphoreSlim` with an initial and maximum count of one only when mutual exclusion must continue across `await`. Await `WaitAsync`, release the semaphore in `finally`, and dispose an owned semaphore with its containing object (Microsoft, n.d.-c).

#### Correct

```csharp
public sealed class StateRefresher : IDisposable
{
    private const int MutualExclusionPermitCount = 1;
    private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(
        MutualExclusionPermitCount,
        MutualExclusionPermitCount);

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshSemaphore.WaitAsync(cancellationToken);
        try
        {
            await RefreshStateAsync(cancellationToken);
        }
        finally
        {
            _refreshSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _refreshSemaphore.Dispose();
    }
}
```

The owned semaphore is released after every wait and disposed with its containing object.

#### Incorrect

```csharp
public async Task RefreshAsync(CancellationToken cancellationToken)
{
    await _refreshSemaphore.WaitAsync(cancellationToken);
    await RefreshStateAsync(cancellationToken);
    _refreshSemaphore.Release();
}
```

An exception can skip `Release` and prevent later callers from entering.

### 5.22 Atomic Operations

Use `Interlocked` for one atomic field update. A sequence of operations that forms one invariant still requires explicit synchronization (Microsoft, n.d.-d).

#### Correct

```csharp
public bool BeginSweep()
{
    const int SweepIdle = 0;
    const int SweepInProgress = 1;

    int previousValue = Interlocked.Exchange(ref _isSweeping, SweepInProgress);
    return previousValue == SweepIdle;
}
```

The exchange changes and reads the shared flag as one atomic operation.

#### Incorrect

```csharp
public bool BeginSweep()
{
    if (_isSweeping == 1)
    {
        return false;
    }

    _isSweeping = 1;
    return true;
}
```

Two threads can read zero before either thread writes the new value.

### 5.23 Concurrent Collections

Use a concurrent collection when independent collection operations occur from multiple threads. Use explicit synchronization when several operations must preserve one shared invariant (Microsoft, n.d.-e).

#### Correct

```csharp
public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions =
        new ConcurrentDictionary<Guid, Session>();

    public bool Register(Session session)
    {
        return _sessions.TryAdd(session.Id, session);
    }
}
```

The concurrent collection makes the independent registration operation thread-safe.

#### Incorrect

```csharp
public sealed class SessionRegistry
{
    private readonly Dictionary<Guid, Session> _sessions =
        new Dictionary<Guid, Session>();

    public void Register(Session session)
    {
        _sessions.Add(session.Id, session);
    }
}
```

The dictionary can be modified simultaneously because the method has no synchronization.

### 5.24 You Aren't Gonna Need It (YAGNI)

Apply the **You Aren't Gonna Need It (YAGNI)** principle by implementing only the types,
operations, abstractions, and extension points required by the current task (Microsoft, 2023c).
Do not add speculative interfaces, configuration flags, or unused branches for hypothetical
future requirements; introduce abstractions only when an actual requirement or second concrete
implementation justifies them.

#### Correct

```csharp
internal sealed class MatchTimeoutMonitor
{
    private readonly int _turnDurationSeconds;

    internal MatchTimeoutMonitor(int turnDurationSeconds)
    {
        _turnDurationSeconds = turnDurationSeconds;
    }

    internal bool IsExpired(DateTime turnStartedAtUtc, DateTime utcNow)
    {
        return (utcNow - turnStartedAtUtc).TotalSeconds >= _turnDurationSeconds;
    }
}
```

The class models only the timeout behavior required now without speculative extension points.

#### Incorrect

```csharp
internal interface ITurnRuleStrategy
{
    bool Evaluate(DateTime turnStartedAtUtc, DateTime utcNow);
}

internal sealed class ConfigurableTimeoutMonitor
{
    private readonly ITurnRuleStrategy? _customStrategy;
    private readonly bool _enableGracePeriod;
    private readonly int _turnDurationSeconds;

    internal ConfigurableTimeoutMonitor(
        int turnDurationSeconds,
        bool enableGracePeriod = false,
        ITurnRuleStrategy? customStrategy = null)
    {
        _turnDurationSeconds = turnDurationSeconds;
        _enableGracePeriod = enableGracePeriod;
        _customStrategy = customStrategy;
    }

    internal bool IsExpired(DateTime turnStartedAtUtc, DateTime utcNow)
    {
        if (_customStrategy != null)
        {
            return _customStrategy.Evaluate(turnStartedAtUtc, utcNow);
        }
        if (_enableGracePeriod)
        {
            return (utcNow - turnStartedAtUtc).TotalSeconds >= _turnDurationSeconds + 5;
        }

        return (utcNow - turnStartedAtUtc).TotalSeconds >= _turnDurationSeconds;
    }
}
```

The code adds an unrequested strategy interface, unused configuration flag, and dead branch for
hypothetical future rules.

---

## 6. Error Handling, Logging, and Diagnostics

### 6.1 Exception Handling

Reserve exceptions for **unexpected technical failures, broken invariants, and unrecoverable infrastructure errors**; represent expected validation rejections, authorization denials, and domain conflicts through explicit return statuses or result types rather than throwing exceptions for normal control flow (Ecma International, 2023; Microsoft, 2025c):

- Catch the most specific exception type applicable only when local recovery or cleanup is possible; empty `catch` blocks are prohibited, and rethrowing must use `throw;` rather than `throw ex;`.
- Exception filters (`catch (SpecificException ex) when (condition)`) are permitted when only a subset of causes can be recovered locally without unwinding the stack; filter conditions must be side-effect-free.

#### Correct: Specific Recovery and Side-Effect-Free Exception Filter

```csharp
public GameSettings LoadSettingsOrDefault(string settingsPath)
{
    try
    {
        string content = File.ReadAllText(settingsPath);
        return DeserializeSettings(content);
    }
    catch (FileNotFoundException)
    {
        return GameSettings.CreateDefault();
    }
}

public async Task<bool> TryPingServerAsync(
    IServerProbe probe,
    CancellationToken callerToken)
{
    try
    {
        await probe.PingAsync(callerToken);
        return true;
    }
    catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
    {
        _logger.LogWarning("Server probe timed out internally.");
        return false;
    }
}
```

The specific catches recover only from conditions the method can handle locally.

#### Incorrect: Broad Catch Masking Failures and Stack-Resetting Rethrow

```csharp
public GameSettings? LoadSettingsOrDefault(string settingsPath)
{
    try
    {
        return DeserializeSettings(File.ReadAllText(settingsPath));
    }
    catch (Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            throw ex;
        }

        return null;
    }
}
```

The broad catch masks failures and `throw ex` replaces the original stack location.

### 6.2 Custom Exceptions

Prefer standard .NET exception types. Define a custom exception only when no standard type represents the failure and callers need to distinguish that condition programmatically. Expected validation, authorization, and domain outcomes must use explicit statuses or results instead of exceptions (Microsoft, 2025c).

#### Correct

```csharp
public void SetDifficulty(int difficulty)
{
    const int MinimumDifficulty = 1;
    const int MaximumDifficulty = 5;

    if (difficulty < MinimumDifficulty || difficulty > MaximumDifficulty)
    {
        throw new ArgumentOutOfRangeException(nameof(difficulty));
    }
}
```

The standard exception already describes an argument outside its allowed range.

#### Incorrect

```csharp
public sealed class InvalidDifficultyException : Exception
{
}
```

The custom type adds no meaning beyond `ArgumentOutOfRangeException`.

### 6.3 Logging

Emit all diagnostics through constructor-injected `Microsoft.Extensions.Logging.ILogger<T>` stored in a `readonly` field (authorized as a fourth constructor parameter under Section 5.8) (Microsoft, 2026g). Configure logging providers and filters solely in executable composition roots, include at least one persistent provider in released server builds, and never substitute `Console.WriteLine`, `Debug.WriteLine`, or direct OS `EventLog` calls in application classes.

#### Correct

```csharp
public sealed class ArenaSessionCoordinator
{
    private readonly ILogger<ArenaSessionCoordinator> _logger;

    public ArenaSessionCoordinator(ILogger<ArenaSessionCoordinator> logger)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public void StartSession(Guid sessionId)
    {
        _logger.LogInformation("Arena session {SessionId} started.", sessionId);
    }
}
```

The injected logger receives a static template with a named placeholder.

#### Incorrect

```csharp
public sealed class ArenaSessionCoordinator
{
    public void StartSession(Guid sessionId)
    {
        Console.WriteLine("Arena session " + sessionId + " started.");
    }
}
```

Direct console output bypasses the configured logging pipeline.

### 6.4 Log Levels

Assign `LogLevel` strictly by technical impact, logging each unhandled failure once at the responsible boundary rather than repeating it across layers (Microsoft, 2026g):

| Level | Technical Usage |
| --- | --- |
| `Trace` / `Debug` | Granular step or development state; disabled by default in released builds. |
| `Information` | Significant lifecycle milestones; not emitted per routine call or validation check. |
| `Warning` | Abnormal or recoverable condition (such as transient retry or timeout) where execution continues safely. |
| `Error` | Current operation failed unexpectedly, while the process continues serving other operations. |
| `Critical` | Process-threatening failure, startup abort, or data-integrity hazard. |

#### Correct

```csharp
_logger.LogInformation(
    "Arena session {SessionId} completed.",
    sessionId);

_logger.LogWarning(
    "Client {SessionId} missed heartbeat ({ElapsedMs} ms); awaiting reconnect.",
    sessionId,
    elapsedMilliseconds);

_logger.LogError(
    exception,
    "Failed to persist reward summary for session {SessionId}.",
    sessionId);
```

Each message uses the level that matches its technical impact.

#### Incorrect

```csharp
_logger.LogCritical("Player {AccountId} opened audio settings.", accountId);
_logger.LogInformation("Database transaction failed.");
```

The levels invert routine activity and an operation failure.

### 6.5 Log Message Conventions

Write English static log message templates (`CA2254`) with **PascalCase** named placeholders (`{SessionId}`, `{CorrelationId}`), pass `Exception` instances to the dedicated logger parameter rather than interpolating `exception.Message`, and propagate correlation identifiers via `BeginScope` across process boundaries (Microsoft, 2026c; Microsoft, 2026g). Never log passwords, hashes, salts, session tokens, verification codes, connection strings, cryptographic keys, or unredacted personal data.

#### Correct

```csharp
public void LogPersistenceFailure(
    Exception exception,
    Guid sessionId,
    string correlationId)
{
    using IDisposable? scope = _logger.BeginScope(
        "CorrelationId:{CorrelationId}",
        correlationId);

    _logger.LogError(
        exception,
        "Failed to save state for session {SessionId}.",
        sessionId);
}
```

The structured template preserves the exception and correlation context without secrets.

#### Incorrect

```csharp
_logger.LogError(
    $"Login for {emailAddress} with password {password} and token {token} " +
    $"failed: {exception.Message}");
```

The interpolated message exposes credentials and discards structured exception data.

### 6.6 Assertions

Use `Debug.Assert` solely to document and check internal development invariants that are always true in correct code; never rely on assertions to validate external input, files, or network responses because `Debug` calls are omitted from Release builds (Microsoft, 2025a).

#### Correct

```csharp
Debug.Assert(
    player.MaximumHealth > 0,
    "Player maximum health invariant must be positive.");
```

The assertion checks an invariant controlled by the program during development.

#### Incorrect

```csharp
Debug.Assert(File.Exists(saveFilePath), "Save file must exist on disk.");
```

An external file can be absent in Release and requires normal error handling.

### 6.7 Player-Facing Error Messages

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

### 6.8 Unhandled Exceptions and Diagnostic Reporting

Configure independent last-chance exception hooks on client and server executables (`DispatcherUnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`), and implement a centralized WCF `IErrorHandler` on server endpoints (Microsoft, 2021c):

- `ProvideFault` must pass declared contractual `FaultException<TFault>` instances through unchanged and convert any unexpected exception into a sanitized `FaultException<ServiceFault>` carrying only a stable error code and correlation identifier.
- `HandleError` must log unexpected exceptions once through `ILogger` (`Error` or `Critical`) without leaking server internals to clients. Background timers and tasks outside WCF operation contexts must maintain their own exception boundaries because `IErrorHandler` covers only WCF dispatch operations.

#### Correct

```csharp
public sealed class ServiceErrorHandler : IErrorHandler
{
    private readonly ILogger<ServiceErrorHandler> _logger;

    public ServiceErrorHandler(ILogger<ServiceErrorHandler> logger)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
    {
        if (error is FaultException)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        ServiceFault detail = new ServiceFault
        {
            ErrorCode = "UnexpectedServiceError",
            CorrelationId = correlationId
        };
        FaultException<ServiceFault> faultException =
            new FaultException<ServiceFault>(detail, new FaultReason("Service error."));
        MessageFault messageFault = faultException.CreateMessageFault();
        fault = Message.CreateMessage(version, messageFault, faultException.Action);
    }

    public bool HandleError(Exception error)
    {
        if (error is not FaultException)
        {
            _logger.LogError(error, "Unhandled WCF service operation failure.");
        }

        return true;
    }
}
```

The handler sanitizes unexpected faults and logs them at the WCF boundary.

#### Incorrect

```csharp
public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
{
    // Exposes internal exception type, message, and stack trace to remote callers.
    FaultException<string> leakedFault =
        new FaultException<string>(error.ToString(), new FaultReason(error.Message));
    fault = Message.CreateMessage(
        version,
        leakedFault.CreateMessageFault(),
        leakedFault.Action);
}
```

The fault exposes the server exception and stack information to the remote caller.

---

## 7. Comments and Documentation

### 7.1 General Comment Rules

Write single-line `//` comments on their own line above the relevant code, formatted as complete English sentences starting with an uppercase letter and ending with a period (except structural test markers `// Arrange`, `// Act`, and `// Assert`). Do not place trailing comments at the end of statements or restate self-evident code (Microsoft, 2025f).

#### Correct

```csharp
// Prevent movement while the player is interacting with the pause menu.
if (isGamePaused)
{
    return;
}
```

The comment explains why the guard exists and follows the required sentence form.

#### Incorrect

```csharp
if (isGamePaused)
{
    return; // return if paused
}
```

The trailing comment repeats the statement instead of explaining intent.

### 7.2 Implementation Comments

Use `//` implementation comments only to explain **why** a non-obvious constraint, workaround, or algorithmic decision exists; avoid `/* ... */` multi-line block comments and line-by-line narration (Microsoft, 2025f).

#### Correct

```csharp
// Reset vertical velocity before applying the jump impulse so mid-air
// double jumps produce a consistent upward height.
playerVelocity.Y = 0f;
playerVelocity.Y += jumpImpulse;
```

The comment records the reason for resetting velocity before the impulse.

#### Incorrect

```csharp
// Set Y velocity to zero.
playerVelocity.Y = 0f;
// Add jump impulse.
playerVelocity.Y += jumpImpulse;
```

The comments narrate assignments that are already clear from the code.

### 7.3 XML Documentation Comments

Write well-formed `///` XML documentation immediately above documented types and members, including at minimum a complete-sentence `<summary>` and applicable `<param>`, `<returns>`, `<exception>`, or `<remarks>` tags matching the exact signature (Microsoft, 2025g, 2026k).

#### Correct

```csharp
/// <summary>
/// Calculates the final damage dealt by an attack.
/// </summary>
/// <param name="baseDamage">The unmitigated base damage.</param>
/// <param name="multiplier">The critical or combo multiplier.</param>
/// <returns>The total calculated damage.</returns>
public float CalculateDamage(float baseDamage, float multiplier)
{
    return baseDamage * multiplier;
}
```

The XML tags match the signature and describe the public contract.

#### Incorrect

```csharp
/// Calculates damage
/// <param name="damage">Damage value</param>
public float CalculateDamage(float baseDamage, float multiplier)
{
    return baseDamage * multiplier;
}
```

The documentation omits required tags and names a parameter that does not exist.

### 7.4 Public API Documentation

XML documentation produces information for API consumers, and Microsoft recommends it for publicly visible APIs. For this project, detailed XML documentation is required only for types and members that form an externally consumed service or library boundary. Public implementation types used only between solution assemblies should remain undocumented when their identifiers and signatures already explain their behavior. Add documentation when a constraint, side effect, or failure cannot be understood from the declaration. Concrete boundary implementations may use `/// <inheritdoc />` when their documented interface already describes the complete contract. Public test types and methods are exempt (Microsoft, 2025i, 2026k).

#### Correct

```csharp
public sealed class MatchApplicationService
{
    public MatchStatus PlacePiece(MatchPlacement placement)
    {
        return MatchStatus.Success;
    }
}
```

The internal application boundary remains readable without comments that repeat its identifiers.

#### Incorrect

```csharp
/// <summary>
/// Provides match application services.
/// </summary>
public sealed class MatchApplicationService
{
    /// <summary>
    /// Places a piece.
    /// </summary>
    public MatchStatus PlacePiece(MatchPlacement placement)
    {
        return MatchStatus.Success;
    }
}
```

The comments repeat the type and method names without adding information for an external consumer.

### 7.5 `TODO` Comments

Mark intentionally deferred implementation work using uppercase `// TODO: Specific pending action.` with sufficient detail for any contributor to execute it; vague notes (`fix later`, `finish`) are prohibited (Microsoft, 2025l).

#### Correct

```csharp
// TODO: Replace default spawn coordinates with stage configuration table entries.
SpawnEnemies(defaultSpawnPositions);
```

The TODO identifies a specific replacement that another contributor can implement.

#### Incorrect

```csharp
// todo: finish later
SpawnEnemies(defaultSpawnPositions);
```

The note does not describe the missing work.

### 7.6 `FIXME` Comments

Mark known defective or temporary behavior requiring correction using uppercase `// FIXME: Specific defect and trigger condition.` so it can be tracked alongside Task List tokens (Microsoft, 2025l).

#### Correct

```csharp
// FIXME: Collision damage triggers twice when two hitboxes overlap in the same frame.
ApplyCollisionDamage(impactDamage);
```

The FIXME identifies the defect and the condition that triggers it.

#### Incorrect

```csharp
// FIXME: broken
ApplyCollisionDamage(impactDamage);
```

The note gives no information needed to reproduce the defect.

---

## 8. Input Validation and Security

### 8.1 Trust Boundaries

Treat data received from outside the current trust boundary as untrusted (Microsoft, 2023j; OWASP Foundation, n.d.-b):

- Preventive WPF client checks never replace authoritative server validation and authorization.
- Resolve caller identity and permissions exclusively from a server-validated session token rather than client-supplied account IDs, usernames, or role flags.
- Enforce domain and authorization policies inside application services before mutating state, returning expected rejection statuses (Section 6.1) mapped to declared WCF faults at the service adapter.

#### Correct

```csharp
public async Task<LoadoutRenameStatus> RenameLoadoutAsync(
    string sessionToken,
    int slotIndex,
    string proposedName)
{
    SessionIdentity? caller = await _sessionValidator.GetActiveCallerAsync(sessionToken);
    if (caller is null)
    {
        return LoadoutRenameStatus.Unauthorized;
    }

    LoadoutNameResult nameResult = LoadoutNamePolicy.Normalize(proposedName);
    if (nameResult.Status != LoadoutNameStatus.Success)
    {
        return LoadoutRenameStatus.InvalidName;
    }

    await _loadoutRepository.RenameSlotAsync(
        caller.AccountId,
        slotIndex,
        nameResult.GetValue());
    return LoadoutRenameStatus.Success;
}
```

The server derives identity from the session and validates the name before persistence.

#### Incorrect

```csharp
public async Task RenameLoadoutAsync(long clientSuppliedAccountId, int slotIndex, string name)
{
    // Trusts the account ID sent by the client and assumes the UI validated the name.
    await _loadoutRepository.RenameSlotAsync(clientSuppliedAccountId, slotIndex, name);
}
```

The method trusts caller identity and validation supplied by the client.

### 8.2 Input Validation

Validate input presence, length, format, allowlisted values, inclusive numeric bounds, and domain invariants before executing any dependent operation (OWASP Foundation, n.d.-b; The MITRE Corporation, n.d.):

- Apply field-specific normalization consistently before validation (for example, trimming display names while never trimming passwords) without silently clamping or truncating invalid input unless explicitly specified.
- Return explicit validation statuses for expected input rejections while allowing unexpected repository or infrastructure exceptions to propagate to the error boundary (Section 6.1) rather than masking them as fake result codes.

#### Correct

```csharp
public async Task<HighScoreQueryResult> GetTopScoresAsync(int maximumEntries)
{
    if (maximumEntries < 1 || maximumEntries > MaximumAllowedEntries)
    {
        return HighScoreQueryResult.InvalidMaximumEntries();
    }

    IReadOnlyList<HighScoreEntry> entries =
        await _highScoreRepository.GetTopScoresAsync(maximumEntries);
    return HighScoreQueryResult.Success(entries);
}
```

The method validates the complete range before querying the repository.

#### Incorrect

```csharp
public async Task<HighScoreQueryResult> GetTopScoresAsync(int maximumEntries)
{
    if (maximumEntries < 1)
    {
        return HighScoreQueryResult.InvalidMaximumEntries();
    }

    try
    {
        IReadOnlyList<HighScoreEntry> entries =
            await _highScoreRepository.GetTopScoresAsync(maximumEntries);
        return HighScoreQueryResult.Success(entries);
    }
    catch (Exception)
    {
        return HighScoreQueryResult.ServiceUnavailable();
    }
}
```

The method omits the upper bound and converts technical failures into a false domain result.

### 8.3 File System Access

Confine file reads, writes, and deletions to server-controlled root directories using server-generated GUID filenames and allowlisted extensions (Microsoft, n.d.-a; OWASP Foundation, n.d.-a):

- Never pass client-supplied paths or raw filenames to `Path.Combine` without rejecting rooted paths, directory separators, and traversal segments (`..`).
- Validate encoded byte size, decoded image headers, and pixel dimensions before storing uploaded media, dispose streams per Section 5.16, and never expose physical server paths in contracts or error messages.

#### Correct

```csharp
private string ResolveAvatarFilePath(string storedReference)
{
    if (string.IsNullOrEmpty(storedReference)
        || !storedReference.StartsWith(ReferencePrefix, StringComparison.Ordinal))
    {
        throw new IOException("The stored avatar reference is invalid.");
    }

    string fileName = storedReference.Substring(ReferencePrefix.Length);
    string extension = Path.GetExtension(fileName);
    string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

    if ((extension != ".png" && extension != ".jpg")
        || !Guid.TryParseExact(nameWithoutExtension, "N", out Guid _)
        || fileName != Path.GetFileName(fileName))
    {
        throw new IOException("The stored avatar reference is invalid.");
    }

    return Path.Combine(_avatarRootDirectory, fileName);
}
```

The method accepts only a validated stored reference beneath the controlled root.

#### Incorrect

```csharp
private string ResolveAvatarFilePath(string clientProvidedPath)
{
    return Path.Combine(_avatarRootDirectory, clientProvidedPath);
}
```

The client-controlled path can escape the intended storage root.

### 8.4 Credential and Secret Management

Keep database connection strings and other secrets in server environment variables or untracked local configuration. Never place them in source code, client builds, logs, or version control (OWASP Foundation, n.d.-c, n.d.-d):

- Fail server startup immediately when required secrets are missing or undersized rather than substituting fallback keys, and clear temporary decoded key buffers after initialization.
- Hash passwords on the server with a salted cryptographic password hash (such as PBKDF2 with random salt), store verification codes as purpose-bound keyed digests, validate unpredictable session tokens against server state, and immediately revoke and rotate any exposed credential.

#### Correct

```csharp
private static TokenDigestProtector CreateTokenProtector()
{
    byte[] keyBytes = Convert.FromBase64String(
        EnvironmentSettings.ReadRequired("GAME_TOKEN_SIGNING_KEY"));
    try
    {
        return new TokenDigestProtector(keyBytes);
    }
    finally
    {
        Array.Clear(keyBytes, 0, keyBytes.Length);
    }
}
```

The secret comes from external configuration and its temporary byte array is cleared.

#### Incorrect

```csharp
private static TokenDigestProtector CreateTokenProtector()
{
    const int TokenKeySizeInBytes = 32;
    byte[] fallbackKeyBytes = new byte[TokenKeySizeInBytes];
    return new TokenDigestProtector(fallbackKeyBytes);
}
```

The predictable fallback key makes every generated digest reproducible by an attacker.

---

## 9. Testing and Quality Assurance

### 9.1 Testing Strategy

Verify new or modified behavior at the lowest practical automated testing level: prefer fast, isolated unit tests for domain rules and application logic, and use integration tests where correctness depends on multiple real components or infrastructure collaborating together (Microsoft, 2026m; Reese, 2025).

#### Correct

```text
Feature: Player combat

Unit tests:
- DamageCalculatorTests
- CriticalHitCalculatorTests

Integration tests:
- CombatResolutionIntegrationTests (verifies Weapon, DamageCalculator, and CombatantHealth together)
```

The plan assigns isolated rules to unit tests and real collaboration to an integration test.

#### Incorrect

```text
Feature: Player combat

Testing:
- Launch the desktop client manually, click Attack once, and visually check the health bar.
```

The manual check is not deterministic or self-verifying.

### 9.2 Unit Testing

For this project, unit test projects must reference `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, and `MSTest.TestFramework`. Test classes must use `[TestClass]`, and test methods must use `[TestMethod]`, so Visual Studio Test Explorer and `dotnet test` can discover and run an individual test. Each unit test must be fast, deterministic, self-checking via `Assert`, and isolated from file systems, databases, and networks by substituting controlled `Stub` or `Fake` doubles for external dependencies (Microsoft, 2026m, 2026o, 2026r; Reese, 2025).

#### Correct

```csharp
[TestClass]
public sealed class DamageCalculatorTests
{
    [TestMethod]
    public void CalculateDamage_ValidMultiplier_ReturnsScaledDamage()
    {
        // Arrange
        const int BaseDamage = 20;
        const int DamageMultiplier = 2;
        const int ExpectedDamage = 40;
        DamageCalculator calculator = new DamageCalculator();

        // Act
        int actualDamage = calculator.CalculateDamage(BaseDamage, DamageMultiplier);

        // Assert
        Assert.AreEqual(ExpectedDamage, actualDamage);
    }
}
```

The MSTest attributes make the isolated test discoverable and independently executable.

#### Incorrect

```csharp
public static class DamageCalculatorChecks
{
    public static void RunDamageCheck()
    {
        const int BaseDamage = 20;
        const int DamageMultiplier = 2;
        const int ExpectedDamage = 40;
        DamageCalculator calculator = new DamageCalculator();

        int actualDamage = calculator.CalculateDamage(
            BaseDamage,
            DamageMultiplier);

        Assert.AreEqual(ExpectedDamage, actualDamage);
    }
}
```

The custom static runner cannot be discovered or executed individually by MSTest.

### 9.3 Integration Testing

Integration tests must exercise real interactions across collaborating components (such as repositories, serialization, or temporary storage) and deterministically clean up created resources in `finally` or disposal scopes rather than replacing the tested integration boundary with mocks (Microsoft, 2026m; van der Til et al., 2026).

#### Correct

```csharp
[TestClass]
public sealed class SaveGameIntegrationTests
{
    [TestMethod]
    public void SaveAndLoad_ValidGameState_RestoresPlayerHealth()
    {
        // Arrange
        string savePath = Path.GetTempFileName();
        try
        {
            const int ExpectedPlayerHealth = 75;
            JsonSaveRepository repository = new JsonSaveRepository(savePath);
            SaveGameApplicationService service = new SaveGameApplicationService(repository);
            GameState expectedState = new GameState
            {
                PlayerHealth = ExpectedPlayerHealth
            };

            // Act
            service.Save(expectedState);
            GameState loadedState = service.Load();

            // Assert
            Assert.AreEqual(ExpectedPlayerHealth, loadedState.PlayerHealth);
        }
        finally
        {
            File.Delete(savePath);
        }
    }
}
```

The test uses real temporary storage and removes it in `finally`.

#### Incorrect

```csharp
[TestClass]
public sealed class SaveGameIntegrationTests
{
    [TestMethod]
    public void SaveAndLoad_ValidGameState_RestoresPlayerHealth()
    {
        const int SavedPlayerHealth = 75;

        // Replaces the persistence component with an in-memory stub while claiming
        // to test persistence integration.
        InMemorySaveRepositoryStub stub = new InMemorySaveRepositoryStub(SavedPlayerHealth);
        SaveGameApplicationService service = new SaveGameApplicationService(stub);

        GameState loadedState = service.Load();

        Assert.AreEqual(SavedPlayerHealth, loadedState.PlayerHealth);
    }
}
```

The stub removes the persistence boundary that the integration test claims to verify.

### 9.4 Arrange-Act-Assert Structure

Structure every test into three blank-line-separated phases labeled with `// Arrange`, `// Act`, and `// Assert`, executing a **single `Act` operation** per test method so failures pinpoint one behavior (Microsoft, 2026o; Reese, 2025).

#### Correct

```csharp
[TestClass]
public sealed class PlayerHealthTests
{
    [TestMethod]
    public void TakeDamage_ValidAmount_DecreasesCurrentHealth()
    {
        // Arrange
        const int InitialHealth = 100;
        const int DamageAmount = 30;
        const int ExpectedHealth = 70;
        PlayerHealth playerHealth = new PlayerHealth(InitialHealth);

        // Act
        playerHealth.TakeDamage(DamageAmount);

        // Assert
        Assert.AreEqual(ExpectedHealth, playerHealth.CurrentHealth);
    }
}
```

The labeled phases contain one action followed by one focused assertion.

#### Incorrect

```csharp
[TestClass]
public sealed class PlayerHealthTests
{
    [TestMethod]
    public void TakeDamage_ValidAmount_DecreasesCurrentHealth()
    {
        const int InitialHealth = 100;
        const int FirstDamageAmount = 30;
        const int SecondDamageAmount = 10;
        const int ExpectedHealth = 60;
        PlayerHealth playerHealth = new PlayerHealth(InitialHealth);
        playerHealth.TakeDamage(FirstDamageAmount);
        playerHealth.TakeDamage(SecondDamageAmount);
        Assert.AreEqual(ExpectedHealth, playerHealth.CurrentHealth);
    }
}
```

The test performs the action twice and does not separate its phases.

---

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

GitLab. (n.d.). *JavaScript style guide*. GitLab Docs. Retrieved August 29, 2026, from [https://docs.gitlab.com/development/fe_guide/style/javascript/](https://docs.gitlab.com/development/fe_guide/style/javascript/)

Google. (n.d.). *C# at Google Style Guide*. Google Style Guides. Retrieved August 29, 2026, from [https://google.github.io/styleguide/csharp-style.html](https://google.github.io/styleguide/csharp-style.html)

JetBrains. (2026, March 31). *Invert ‘if’ statement to reduce nesting*. Inspectopedia Documentation. [https://www.jetbrains.com/help/inspectopedia/InvertIf.html](https://www.jetbrains.com/help/inspectopedia/InvertIf.html)

Microsoft. (n.d.-a). *Path.Combine method (System.IO)*. Microsoft Learn. Retrieved October 22, 2026, from [https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine)

Microsoft. (2020a, October 14). *No-tracking queries*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking](https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking)

Microsoft. (2020b, October 14). *Querying and finding entities*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/](https://learn.microsoft.com/en-us/ef/ef6/querying/)

Microsoft. (2020c, October 14). *Working with DbContext*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext)

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

Microsoft. (2023e, October 3). *Names of namespaces*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces)

Microsoft. (2023f, October 3). *Names of type members*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members)

Microsoft. (2023g, October 3). *Naming parameters*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-parameters](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-parameters)

Microsoft. (2023h, July 25). *.NET formatting options*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/dotnet-formatting-options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/dotnet-formatting-options)

Microsoft. (2023i, December 16). *Remove unnecessary using directives (IDE0005)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0005](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0005)

Microsoft. (2023j, March 30). *Securing services*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/framework/wcf/securing-services](https://learn.microsoft.com/en-us/dotnet/framework/wcf/securing-services)

Microsoft. (2023k, July 27). *IDE0019: Use pattern matching to avoid 'as' followed by a 'null' check*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0019](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0019)

Microsoft. (2024, January 18). *Use pattern matching to avoid 'is' check followed by a cast (IDE0020 and IDE0038)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020-ide0038)

Microsoft. (2025a, October 30). *Assertions in managed code*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/debugger/assertions-in-managed-code](https://learn.microsoft.com/en-us/visualstudio/debugger/assertions-in-managed-code)

Microsoft. (2025b, August 15). *Asynchronous programming scenarios*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)

Microsoft. (2025c, October 22). *Best practices for exceptions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)

Microsoft. (2025d, January 30). *C# formatting options*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options)

Microsoft. (2025e, October 30). *Code metrics: Cyclomatic complexity*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity)

Microsoft. (2025f, January 18). *Common C# code conventions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

Microsoft. (2025g, September 12). *Documentation comments (C# language specification)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments)

Microsoft. (2025h, July 14). *Error message guidelines*. Microsoft Learn. [https://learn.microsoft.com/en-us/windows/win32/debug/error-message-guidelines](https://learn.microsoft.com/en-us/windows/win32/debug/error-message-guidelines)

Microsoft. (2025i, June 17). *How to write good /// docs for .NET API reference*. Microsoft Learn. [https://learn.microsoft.com/en-us/contribute/content/dotnet/api-documentation](https://learn.microsoft.com/en-us/contribute/content/dotnet/api-documentation)

Microsoft. (2025j, May 29). *Names of classes, structs, and interfaces*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces)

Microsoft. (2025k, November 19). *Properties (C# Programming Guide)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties)

Microsoft. (2025l, November 17). *Use Task List with code comments and shortcuts*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list](https://learn.microsoft.com/en-us/visualstudio/ide/using-the-task-list)

Microsoft. (2025m, March 24). *Introduction to LINQ queries in C#*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/introduction-to-linq-queries)

Microsoft. (2025n, February 4). *CA2000: Dispose objects before losing scope (code analysis)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000)

Microsoft. (2026a, July 14). *C# identifier naming rules and conventions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)

Microsoft. (2026b, April 2). *CA1711: Identifiers should not have incorrect suffix (code analysis)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711)

Microsoft. (2026c, April 2). *CA2254: Template should be a static expression*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254)

Microsoft. (2026d, April 13). *Extract and inline refactorings*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/ide/reference/refactoring-extract-inline](https://learn.microsoft.com/en-us/visualstudio/ide/reference/refactoring-extract-inline)

Microsoft. (2026e, January 26). *The `init` keyword (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init)

Microsoft. (2026f, January 24). *Lambda expressions and anonymous functions*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions)

Microsoft. (2026g, February 4). *Logging in C# and .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)

Microsoft. (2026h, May 27). *Namespaces and using directives*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces)

Microsoft. (2026i, May 26). *Nullable reference types*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types)

Microsoft. (2026j, January 26). *`readonly` (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly)

Microsoft. (2026k, June 22). *Recommended XML tags for C# documentation comments*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)

Microsoft. (2026l, June 5). *Records (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)

Microsoft. (2026m, April 27). *Testing in .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/](https://learn.microsoft.com/en-us/dotnet/core/testing/)

Microsoft. (2026n, May 26). *Tutorial: Express your design intent with nullable and non-nullable reference types*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types)

Microsoft. (2026o, June 24). *Write tests with MSTest*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests)

Microsoft. (2026p, March 23). *Handle and raise events*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/events/](https://learn.microsoft.com/en-us/dotnet/standard/events/)

Microsoft. (2026q, January 20). *using statement - ensure the correct use of disposable objects*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)

Microsoft. (2026r, March 9). *Unit test basics with Test Explorer*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics)

Microsoft. (n.d.-b). *The lock statement: ensure exclusive access to a shared resource*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)

Microsoft. (n.d.-c). *Async semaphores, locks, and reader/writer coordination*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced)

Microsoft. (n.d.-d). *Interlocked class*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked)

Microsoft. (n.d.-e). *Thread-safe collections*. Microsoft Learn. Retrieved November 7, 2026, from [https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/](https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/)

OWASP Foundation. (n.d.-a). *File upload cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)

OWASP Foundation. (n.d.-b). *Input validation cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html)

OWASP Foundation. (n.d.-c). *Password storage cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

OWASP Foundation. (n.d.-d). *Secrets management cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

Reese, J. (2025, March 22). *Unit testing best practices for .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

The MITRE Corporation. (n.d.). *CWE-20: Improper input validation*. Common Weakness Enumeration. Retrieved October 22, 2026, from [https://cwe.mitre.org/data/definitions/20.html](https://cwe.mitre.org/data/definitions/20.html)

van der Til, J., Costello, M., & Calvarro Nelson, J. (2026, March 10). *Integration tests in ASP.NET Core*. Microsoft Learn. [https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

---

## 12. AI Use Declaration

Artificial Intelligence tools were used as a support resource during the drafting and refinement of this standard for structural organization, wording conciseness, example synthesis, cross-section consistency checks, and reference verification. All normative rules, examples, and citations were reviewed and verified against official language specifications, framework documentation, and security standards; full technical responsibility for this document remains with the engineering authors.
