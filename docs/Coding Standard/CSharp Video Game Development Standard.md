# C# Video Game Development Standard

Table of contents

---

## 1. Introduction

This development standard defines the coding rules and practices that will be followed during the implementation of the C# video game project. Its purpose is to provide the entire development team with a common way to write, organize, document, test, and maintain source code.

Because this is a school project developed by a team that is learning C#, the standard prioritizes **clarity, consistency, maintainability, and ease of learning** over highly condensed or advanced language techniques. Microsoft states that coding conventions improve readability, consistency, collaboration, maintainability, and extensibility, while also noting that teams may adapt conventions to their own needs (Microsoft, 2025f).

### 1.1 Problem Statement

C# allows developers to express the same behavior in many different valid ways. In a team of developers who are still learning the language, this flexibility can lead to inconsistent naming, different formatting styles, unnecessary duplication, overly complex methods, ambiguous `null` behavior, inconsistent testing practices, and different approaches to error handling.

Many of these differences are not compiler errors. Microsoft explicitly distinguishes between C# language rules and naming or style conventions, which are intended to provide consistency but are not necessarily enforced by the compiler (Microsoft, 2026a). Therefore, the project requires a shared development standard that reduces unnecessary variation and provides a clear reference for code reviews.

### 1.2 Purpose

The purpose of this standard is to establish a set of **clear, verifiable, and consistently applicable development rules** for the project. These rules are intended to:

- Improve source-code readability.
- Make code written by different team members look and behave consistently.
- Reduce unnecessary complexity and duplicated behavior.
- Make errors easier to identify and diagnose.
- Encourage maintainable object-oriented design.
- Define consistent practices for error handling and logging.
- Establish a common structure for automated tests.
- Provide examples that help team members learn how each rule should be applied.
- Allow selected rules to be enforced automatically through compiler diagnostics, analyzers, or `.editorconfig` where possible.

This standard complements the C# language specification and official .NET guidance; it does not replace them.

**Code Example Scope:** Code examples in this standard are intentionally minimal and may omit surrounding namespaces, `using` directives, XML documentation, constant definitions, and unrelated class members when those elements are not relevant to the rule being demonstrated. Code shown as **Correct** must still comply with all substantive rules applicable to the behavior being demonstrated.

---

## 2. Language

### 2.1 Source Code Language

All identifiers created by the project—including class names, methods, properties, fields, variables, parameters, namespaces, comments, XML documentation, and technical log messages—must be written in **English**.

Names must remain descriptive and meaningful instead of being literal or unclear translations. Player-facing text is excluded from this rule because it may be localized according to the language requirements of the game.

Microsoft requires meaningful and descriptive identifiers but does not require English specifically. Therefore, English is a **project convention** adopted to ensure that all team members use one consistent technical vocabulary (Microsoft, 2026a).

#### Correct

```csharp
int remainingLives = 3;
bool isBossDefeated = false;

// Prevent the player from moving during the cutscene.
```

The identifiers and technical comment use a consistent English vocabulary.

#### Incorrect

```csharp
int vidasRestantes = 3;
bool jefeDerrotado = false;

// Prevent player movement during la cinemática.
```

The source code mixes languages, making the project’s technical vocabulary inconsistent.

### 2.2 User-Facing Language and Localization

All user-facing text that must change with the selected culture must be stored in the application's localization resources. The project supports `en-US` and `es-MX`. Resource keys must remain stable, use English technical names, and appear in the internationalization dictionary with the exact same spelling. Images containing user-facing text must have localized alternatives; images without language-dependent content do not require one.

---

## 3. Naming Conventions

### 3.1 General Identifier Rules

Identifiers must use domain meaningful and descriptive names that communicate their purpose without requiring the reader to inspect the implementation. Clarity must be preferred over abbreviations or unnecessarily short names. Identifiers should also avoid two consecutive underscores (`__`), because Microsoft reserves such names for compiler-generated identifiers. Single-letter identifiers should only be used when their purpose is immediately obvious, such as simple loop counters (Microsoft, 2026a).

#### Correct

```csharp
int remainingLives = 3;
```

`remainingLives` clearly describes the information stored by the variable and follows the appropriate casing convention.

#### Incorrect

```csharp
int rl__ = 3;
```

`rl__` is unnecessarily abbreviated, does not clearly communicate its purpose, and contains two consecutive underscores.

### 3.2 Namespaces

Namespace names must use **PascalCase** and organize related code into a clear hierarchy separated by periods. For this school project, the project name should be used as the root namespace, followed by functional areas such as `Gameplay`, `UI`, `Audio`, or `Combat`. Microsoft recommends meaningful hierarchical namespace names, while Unity also recommends PascalCase namespaces and sub-namespaces for organizing game systems (Microsoft, 2023e; Unity Technologies, n.d.-b).

#### Correct

```csharp
namespace GameProject.Gameplay.Combat;
```

The namespace uses PascalCase and clearly identifies the project and the subsystem represented by the code.

#### Incorrect

```csharp
namespace game_project.gameplay_combat;
```

The namespace uses lowercase words and underscores instead of the established PascalCase hierarchy.

### 3.3 Classes

Class names must use **PascalCase**, meaning that the first letter of each word in the name must be uppercase and the words must not be separated by spaces or underscores. Names must be meaningful and descriptive, preferably using a **noun or noun phrase** that clearly represents the responsibility or concept modeled by the class. Class names must not include prefixes such as `C` merely to indicate that the type is a class. (Microsoft, 2025j; Microsoft, 2026a).

#### Correct

```csharp
public class PlayerMovementController
{
}
```

`PlayerMovementController` uses PascalCase and is a descriptive noun phrase that communicates the responsibility represented by the class.

#### Incorrect

```csharp
public class Cplayer_movement_controller
{
}
```

`Cplayer_movement_controller` violates the standard because it does not use PascalCase, uses underscores to separate words, and adds the unnecessary `C` prefix to identify the type as a class.

---

### 3.4 Interfaces

Interface names must use **PascalCase** and begin with the capital letter `I`. The remainder of the name should clearly describe the capability, behavior, or contract represented by the interface. Microsoft recommends adjective phrases or, when appropriate, descriptive nouns or noun phrases (Microsoft, 2025j; Microsoft, 2026a).

#### Correct

```csharp
public interface IDamageable
{
}
```

`IDamageable` uses the required `I` prefix, PascalCase, and describes a capability that implementing objects provide.

#### Incorrect

```csharp
public interface DamageableInterface
{
}
```

The name does not use the standard `I` prefix and unnecessarily includes the word `Interface`.

### 3.5 Enumerations

Enumeration type names must use **PascalCase**. A normal enumeration should use a singular noun because a variable normally represents one value from the set. Enumerations marked with `[Flags]` should use a plural noun. The suffixes `Enum`, `Flag`, and `Flags` must not be added simply to identify the type as an enumeration (Microsoft, 2025j; Microsoft, 2026a).

#### Correct

```csharp
public enum WeaponType
{
    Sword,
    Bow,
    Staff
}
```

`WeaponType` is singular, descriptive, and uses PascalCase.

#### Incorrect

```csharp
public enum WeaponTypeEnum
{
    Sword,
    Bow,
    Staff
}
```

The `Enum` suffix is redundant and explicitly discouraged by Microsoft’s .NET naming guidelines.

### 3.6 Enumeration Members

Enumeration member names must use **PascalCase** and clearly describe the represented value. Enumeration values should not include prefixes that repeat the name or type of the enumeration. Unity’s official C# style guidance explicitly recommends PascalCase for both enumeration types and their values, consistent with Microsoft naming practices (Microsoft, 2025j; Unity Technologies, n.d.-b).

#### Correct

```csharp
public enum AttackType
{
    LightAttack,
    HeavyAttack,
    SpecialAttack
}
```

Each value uses PascalCase and identifies the value without adding an unnecessary type prefix.

#### Incorrect

```csharp
public enum AttackType
{
    attack_light,
    attack_heavy,
    attack_special
}
```

The members do not use PascalCase and contain an unnecessary repeated prefix.

### 3.7 Fields

Private and internal non-constant instance fields must use **camelCase prefixed with an underscore (`_`)**. These conventions make fields distinguishable from local variables and other members while following the current conventions documented by the .NET team (Microsoft, 2026a).

#### Correct

```csharp
private int _currentHealth;
```

The instance field uses `_camelCase`.

#### Incorrect

```csharp
private int CurrentHealth;
```

The private instance field incorrectly uses PascalCase.

### 3.8 Properties

Property names must use **PascalCase** and should normally be nouns, noun phrases, or adjectives because properties represent data or characteristics of an object. Boolean properties should use affirmative names and may use prefixes such as `Is`, `Has`, or `Can` when these prefixes improve clarity (Microsoft, 2023f).

#### Correct

```csharp
public int CurrentHealth { get; private set; }
```

`CurrentHealth` uses PascalCase and clearly identifies the state represented by the property.

#### Incorrect

```csharp
public int current_health { get; private set; }
```

The property uses snake_case instead of PascalCase.

### 3.9 Local Variables

Local variable names must use **camelCase** and should clearly describe the value or object that they contain. Abbreviations and single-letter names should normally be avoided unless their meaning is immediately obvious, such as a simple loop index or mathematical expression (Microsoft, 2026a).

#### Correct

```csharp
int totalDamage = baseDamage + bonusDamage;
```

`totalDamage` follows camelCase and makes the variable’s purpose immediately understandable.

#### Incorrect

```csharp
int Total_Dmg = baseDamage + bonusDamage;
```

The variable does not use camelCase and includes an unnecessary abbreviation and underscore.

### 3.10 Parameters

Method and constructor parameters must use **camelCase** and have descriptive names based on their meaning rather than merely their data type. (Microsoft, 2023g).

#### Correct

```csharp
public void SetMovementSpeed(float movementSpeed)
{
}
```

`movementSpeed` uses camelCase and describes the purpose of the supplied value.

#### Incorrect

```csharp
public void SetMovementSpeed(float ms)
{
}
```

`ms` is unnecessarily abbreviated and does not clearly communicate the parameter’s meaning.

### 3.11 Constants

Constant identifiers must use **PascalCase**, regardless of whether the constant is public, private, or internal. This follows the current naming convention documented by the .NET team and differs from the `UPPER_SNAKE_CASE` convention commonly used in some other programming languages (Microsoft, 2026a).

#### Correct

```csharp
private const int MaximumLives = 3;
```

`MaximumLives` uses PascalCase and clearly identifies the purpose of the constant.

#### Incorrect

```csharp
private const int MAXIMUM_LIVES = 3;
```

Although this style is common in other languages, it does not follow the current Microsoft C#/.NET convention adopted by this standard.

### 3.12 Methods

Method names must use **PascalCase** and should begin with a verb or verb phrase because methods represent actions performed by the program. Names should explain the operation performed instead of describing only the data involved (Microsoft, 2023f; Microsoft, 2026a).

#### Correct

```csharp
public int CalculateDamage(int baseDamage)
{
    return baseDamage;
}
```

`CalculateDamage` uses PascalCase and starts with a verb that describes the action performed.

#### Incorrect

```csharp
public int damage_calculation(int baseDamage)
{
    return baseDamage;
}
```

The method does not use PascalCase and is expressed as a noun rather than an action.

### 3.13 Events

Event names must use **PascalCase** and should be verbs or verb phrases describing the action represented by the event. When separate events represent a state before and after an action, tense should communicate the difference, such as `Closing` and `Closed`, rather than prefixes such as `Before` or `After` (Microsoft, 2023f).

#### Correct

```csharp
public event Action PlayerDied;
```

`PlayerDied` uses PascalCase and clearly indicates an event that occurs after the player’s death.

#### Incorrect

```csharp
public event Action AfterPlayerDeath;
```

The name uses `After` instead of expressing the event through the appropriate tense.

### 3.14 Test Classes

.NET does not prescribe one universal suffix for every test framework. However, Microsoft’s current MSTest documentation uses test classes such as `CalculatorTests`, following the practical pattern **`<TypeUnderTest>Tests`**. This project adopts that pattern so developers can immediately identify which production class a test class verifies (Microsoft, 2026o).

#### Correct

```csharp
public class PlayerHealthTests
{
}
```

The name clearly indicates that the class contains tests for `PlayerHealth`.

#### Incorrect

```csharp
public class TestPlayerStuff
{
}
```

The name does not follow the adopted `<TypeUnderTest>Tests` convention and does not clearly identify the class being tested.

### 3.15 Test Methods

Unit-test method names must contain three or four elements: **the method being tested, the scenario being evaluated, the expected behavior and, if needed, the actual flow being tested (e.g. Failed)**. These parts should be separated with underscores so the purpose of the test can be understood directly from its name. Microsoft recommends this pattern because well-named tests also serve as executable documentation (Reese, 2025). The fourth element in this case is a project convention.

#### Correct

```csharp
public void TakeDamage_HealthAboveZero_DecreasesHealth()
{
}

public void TakeDamage_HealthAboveZero_DecreasesHealth_Failed()
{
}
```

The first name identifies the method, the scenario, and the expected result.

The second name contains all the identifiers from the first name and also includes the flow being tested.

#### Incorrect

```csharp
public void TestDamage()
{
}
```

The name does not explain the scenario being tested or the expected behavior.

### 3.16 Prefixes and Suffixes

Prefixes and suffixes must only be added when they communicate a meaning established by C#/.NET conventions or explicitly defined by this standard. Type information must not be redundantly encoded in names through arbitrary prefixes or suffixes.

The project adopts the following conventions:

| Element | Convention |
| --- | --- |
| Interface | Prefix with `I`, such as `IDamageable`. |
| Descriptive generic type parameter | Prefix with `T`, such as `TEnemy`. |
| Private/internal instance field | Prefix with `_`, such as `_currentHealth`. |
| Thread-static field | Prefix with `t_`. |
| Asynchronous method | Suffix with `Async`. |
| Custom exception | Suffix with `Exception`. |
| Event-data type derived from `EventArgs` | Suffix with `EventArgs`. |
| Event-handler delegate | Suffix with `EventHandler`. |
| Other callback delegate | Suffix with `Callback`. |

Class names must **not** use prefixes such as `C`. The suffixes `Enum`, `Delegate`, `Impl`, `Ex`, and `New` must not be used simply to identify the implementation or type category. Microsoft explicitly establishes several of these conventions and provides analyzer rule `CA1711` for inappropriate suffixes (Microsoft, 2025j; Microsoft, 2026a, 2026b). Asynchronous methods follow the .NET `Async` suffix convention (Microsoft, 2025b).

#### Correct

```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
}

public class EnemyManager
{
    private int _remainingEnemies;

    public Task LoadEnemiesAsync()
    {
        return Task.CompletedTask;
    }
}
```

The prefixes and suffixes communicate meanings established by the project’s C#/.NET conventions.

#### Incorrect

```csharp
public interface DamageableInterface
{
    void TakeDamage(int damage);
}

public class CEnemyManagerImpl
{
    public Task LoadEnemies()
    {
        return Task.CompletedTask;
    }
}

public class DamageCalculationError : Exception
{
}
```

`DamageableInterface` omits the `I` prefix, `CEnemyManagerImpl` introduces a prohibited class prefix and `Impl` suffix, the asynchronous method omits `Async`, and an exception-derived class does not end in `Exception`.

### **3.17 WPF Controls, Resource Keys, and Asset Files**

WPF controls that need a name use camelCase and a descriptive purpose followed by the control type when useful, such as `languageComboBox`, `emailTextBox`, and `createAccountButton`. Controls should be named when code refers to them or when a name is needed to trace them in the internationalization dictionary. Type prefixes such as `btn`, `txt`, and `img` are not used.

View classes and styles use PascalCase, such as `AccountCreationPage` and `CreateAccountButtonStyle`.

Localization resource keys must use the following project naming pattern:

`<Feature>_<Purpose>`

Both `Feature` and `Purpose` must use PascalCase, and the two parts must be separated by a single underscore (`_`).

The `Feature` part identifies the page, dialog, game area, or functional context to which the text belongs. The `Purpose` part describes the semantic purpose of the localized value rather than the literal text displayed to the user.

Examples already used by the project include:

- `AccountCreation_Title`
- `AccountCreation_ConfirmPasswordLabel``
- `Game_CurrentTurnFormat`

For this project:

- Resource keys must use English technical names regardless of the language of the localized value.
- The same key name must exist with exactly the same spelling in every supported localization resource in which that text is required.
- Resource keys must remain stable when only the translated text changes.
- Keys must describe the purpose of the text instead of reproducing the displayed sentence.
- Keys must not contain spaces, hyphens, accented characters, or localized words.
- Keys must not use abbreviated control prefixes such as `btn`, `lbl`, or `txt`.
- The resource key does not need to match the `x:Name` of the WPF control that displays it.
- Keys representing formatted text that contains replacement values such as `{0}` should use the `Format` suffix.
- The number and meaning of format placeholders must remain compatible across every localized version of the same key.
- New keys should reuse the existing feature name when they belong to an established page or functional area instead of introducing alternate names for the same feature.
- A key must not be renamed solely because the wording of its displayed text changes. Renaming a key is appropriate only when the semantic purpose of the resource changes.

The following semantic suffixes should be used when applicable:

| Resource purpose | Naming form | Example |
| --- | --- | --- |
| Window, page, or section title | `<Feature>_Title` | `AccountCreation_Title` |
| Field or control label | `<Feature>_<Meaning>Label` | `AccountCreation_ConfirmPasswordLabel` |
| Button text | `<Feature>_<Action>Button` | `AccountCreation_CreateAccountButton` |
| Informational or validation message | `<Feature>_<Meaning>Message` | `Login_InvalidCredentialsMessage` |
| Text containing runtime replacement values | `<Feature>_<Meaning>Format` | `Game_CurrentTurnFormat` |
| Placeholder or hint text | `<Feature>_<Meaning>Placeholder` | `Login_EmailPlaceholder` |
| Tooltip text | `<Feature>_<Meaning>Tooltip` | `Game_LeaveMatchTooltip` |

The examples in this table define the naming form; a resource should only be created when the corresponding localized text is actually required by the application.

#### Correct

```
AccountCreation_Title
AccountCreation_ConfirmPasswordLabel
Game_CurrentTurnFormat
```

#### Incorrect

```
tituloCrearCuenta
AccountCreation_lblConfirmPassword
Game_ItIsNowTheTurnOfPlayer
```

---

## 4. Code Style and Formatting

### 4.1 General Formatting

All C# source code must follow a **consistent formatting style throughout the project**. Each statement and declaration should normally appear on its own line, and formatting decisions must prioritize readability and make the structure of the code easy to identify.

The team should store shared formatting rules in an `.editorconfig` file whenever possible so that Visual Studio and compatible development tools can apply the same conventions for every team member. Microsoft recommends code conventions specifically to improve readability, consistency, and collaboration, and documents `.editorconfig` as a mechanism for enforcing those conventions (Microsoft, 2025f).

#### Correct

```csharp
int currentHealth = 100;
int maximumHealth = 100;

currentHealth -= receivedDamage;
UpdateHealthBar(currentHealth, maximumHealth);
```

Each declaration and statement appears on its own line, making the sequence of operations easy to identify.

#### Incorrect

```csharp
int currentHealth = 100, maximumHealth = 100; currentHealth -= receivedDamage; UpdateHealthBar(currentHealth, maximumHealth);
```

Multiple declarations and executable statements are placed on the same line, making the code harder to scan and modify.

### 4.2 Indentation

C# code must use **four spaces for each indentation level**. Tab characters must not be stored as indentation in source files. Code inside classes, methods, conditionals, loops, and other blocks must be indented one level deeper than the block that contains it.

Continuation lines must also be indented so that they are visually distinguishable from the original line. Microsoft’s C# conventions use four spaces, explicitly avoid tab characters, and recommend an additional four-space indentation for continuation lines when automatic formatting is unavailable (Microsoft, 2025f).

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

Each nested block increases the indentation by four spaces.

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

Different indentation widths are mixed, so the visual structure does not consistently represent the nesting of the code.

### 4.3 Braces

Curly braces must follow the **Allman style**: every opening brace `{` and closing brace `}` must appear on its own line and be aligned with the declaration or control statement to which it belongs.

Braces must also be used for `if`, `else`, loops, and other control-flow bodies even when the body contains only one statement. Microsoft uses Allman-style braces in its C# conventions, and the official .NET code-style rule `IDE0011` has `csharp_prefer_braces = true` as its default, which prefers braces even for single-line bodies (Microsoft, 2023a; Microsoft, 2025f).

#### Correct

```csharp
if (playerHealth <= 0)
{
    HandlePlayerDeath();
}
else
{
    ContinueGame();
}
```

All braces appear on separate lines, and both branches use braces even though each contains only one statement.

#### Incorrect

```csharp
if (playerHealth <= 0) {
    HandlePlayerDeath();
}
else
    ContinueGame();
```

The opening brace does not follow Allman style, and the `else` branch omits braces.

### 4.4 Line Length

Source-code lines should not exceed **100 characters whenever they can reasonably be wrapped without reducing clarity**. Long URLs, indivisible string literals, generated code, or other content that cannot be meaningfully divided may be treated as exceptions.

Microsoft’s C# documentation uses a shorter 65-character limit specifically for code displayed in documentation, especially on mobile screens; it does not define that value as a general production C# requirement. For this project, the 100-column limit is therefore adopted from Google’s official C# Style Guide, which establishes a 100-column limit for C# source code developed under its style standard (Google, n.d.; Microsoft, 2025f).

#### Correct

```csharp
PlayerAttackResult attackResult = combatSystem.CalculateAttack(
    player,
    selectedWeapon,
    targetEnemy);
```

The operation is divided into readable lines instead of forcing all arguments onto one long line.

#### Incorrect

```csharp
PlayerAttackResult powerAttackResult = combatSystem.CalculateAttack(player, selectedWeapon, targetEnemy);
```

The statement unnecessarily extends beyond the project’s 100-column limit instead of being wrapped into readable continuation lines.

### 4.5 Line Wrapping

When a statement becomes too long to read comfortably or would exceed the project’s line-length limit, it must be divided into multiple lines at a **logical syntactic boundary**.

Continuation lines must be indented by at least four additional spaces. When an expression containing binary operators must be wrapped, the line break should occur **before the binary operator**. For long method calls, arguments may be placed on continuation lines in a consistent arrangement that makes their relationship to the method call clear (Google, n.d.; Microsoft, 2025f).

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

Continuation lines are clearly indented, and binary operators begin the continued conditions.

#### Incorrect

```csharp
bool canPerformAttack = player.IsAlive &&
player.HasWeapon && player.Stamina >=
MinimumAttackStamina;

PerformAttack(player,
selectedWeapon, targetEnemy);
```

The continuation indentation is inconsistent and the wrapping does not clearly communicate the structure of the expressions or arguments.

---

### 4.6 Horizontal Whitespace

Whitespace within a line must be used consistently to separate language elements and improve readability.

The project must apply the following formatting rules:

- Place one space after control-flow keywords such as `if`, `for`, `while`, and `switch`.
- Place one space before and after binary operators such as `=`, `+`,  , `==`, `&&`, and `||`.
- Place one space after a comma and no space before it.
- Do not place a space between a method name and its opening parenthesis.
- Do not place spaces immediately inside ordinary parentheses.
- Do not place spaces before or after the member-access operator `.`.
- Do not leave unnecessary trailing whitespace at the end of a line.

These choices correspond to Microsoft’s documented C# formatting options and their standard/default formatting values (Microsoft, 2025d).

#### Correct

```csharp
if (currentHealth > 0 && hasControl)
{
    playerPosition = CalculatePosition(x, y);
}
```

The condition, binary operators, comma, method invocation, and parentheses use consistent whitespace.

#### Incorrect

```csharp
if(currentHealth>0&&hasControl)
{
    playerPosition=CalculatePosition (x ,y);
}
```

Spaces are missing around operators and after the `if` keyword, while unnecessary spaces appear before the method parenthesis and comma.

### 4.7 Vertical Whitespace

Blank lines must be used to visually separate **logical sections of code**, not to artificially spread related statements apart.

At least one blank line must separate method definitions and property definitions. Related statements inside a method should normally remain together, while a blank line may be used to separate distinct stages of an operation when doing so improves readability. Excessive empty lines should be avoided.

Microsoft’s C# layout conventions explicitly recommend at least one blank line between method and property definitions and describe layout as a mechanism for emphasizing code structure and improving readability (Microsoft, 2025f).

#### Correct

```csharp
public void TakeDamage(int damage)
{
    CurrentHealth -= damage;
    CurrentHealth = Math.Max(CurrentHealth, 0);

    UpdateHealthBar();
}

public void RestoreHealth()
{
    CurrentHealth = MaximumHealth;

    UpdateHealthBar();
}
```

A blank line separates distinct stages inside the methods and another separates the two method definitions.

#### Incorrect

```csharp
public void TakeDamage(int damage)
{

    CurrentHealth -= damage;

    CurrentHealth = Math.Max(CurrentHealth, 0);

    UpdateHealthBar();
}
public void RestoreHealth()
{
    CurrentHealth = MaximumHealth;
    UpdateHealthBar();
}
```

Excessive blank lines break apart closely related operations, while the two method definitions are not visually separated.

### 4.8 Using Directives

`using` directives must be placed **at the top of the source file and outside the namespace declaration**. Unused `using` directives must be removed.

`System` namespaces should be sorted alphabetically and placed before non-`System` namespaces. Other directives should also be organized consistently. The project’s `.editorconfig` should use `csharp_using_directive_placement = outside_namespace` and may enable `dotnet_sort_system_directives_first = true`.

Microsoft’s current C# conventions recommend placing `using` directives outside namespace declarations because this makes namespace resolution clearer. The official `IDE0065` rule uses `outside_namespace` as its default preference, `IDE0005` detects unnecessary directives, and Microsoft’s formatting options support placing `System.*` imports first (Microsoft, 2022a; Microsoft, 2023h, 2023i; Microsoft, 2025f).

#### Correct

```csharp
using System;
using System.Collections.Generic;

using GameProject.Gameplay;
using GameProject.UI;

namespace GameProject.Combat;

public class CombatManager
{
    private readonly List<Enemy> _enemies = new List<Enemy>();
    private readonly CombatHud _combatHud;

    public CombatManager(CombatHud combatHud)
    {
        _combatHud = combatHud;
    }

    public void StartCombat()
    {
        Console.WriteLine(_enemies.Count);
        _combatHud.Show();
    }
}
```

The directives appear before the namespace and are actively used by the file. `System` namespaces are placed first and sorted before the project namespaces.

#### Incorrect

```csharp
namespace GameProject.Combat;

using GameProject.UI;
using System.Collections.Generic;
using System;
using System.IO;

public class CombatManager
{
    // Implementation.
}
```

The directives are placed after the namespace declaration, are not consistently ordered, and `System.IO` violates the rule if the file does not use anything from that namespace.

### 4.9 Namespace Declarations

Every project type must belong to an appropriate namespace. Namespace names should represent a logical grouping of related types and should normally correspond to the project’s organizational structure.

For new code in this project, **file-scoped namespaces** must be preferred when all types in a source file belong to the same namespace:

```csharp
namespace GameProject.Gameplay.Combat;
```

File-scoped namespaces eliminate an unnecessary nesting level and indentation. Block-scoped namespaces should only be used when a file genuinely requires more than one namespace. Microsoft currently recommends file-scoped namespaces for new C# code and notes that they are the style favored by most modern .NET templates and analyzers (Microsoft, 2026h).

#### Correct

```csharp
using System;

namespace GameProject.Gameplay.Combat;

public class DamageCalculator
{
    public int CalculateDamage(int baseDamage)
    {
        return Math.Max(baseDamage, 0);
    }
}
```

The file uses one clear file-scoped namespace and does not introduce an unnecessary additional indentation level.

#### Incorrect

```csharp
namespace GameProject
{
    namespace Gameplay
    {
        namespace Combat
        {
            public class DamageCalculator
            {
            }
        }
    }
}
```

The namespaces are unnecessarily nested lexically, adding indentation and making the file harder to read when a single file-scoped namespace can represent the same hierarchy.

### 4.10 Early Returns

Early returns should be used when they make the main execution path of a method easier to read by handling **invalid, exceptional, or no-operation conditions before the primary logic**.

A method should normally check its guard conditions near the beginning and return immediately when continuing the operation is unnecessary or inappropriate. This approach should be preferred over wrapping the entire normal execution path inside multiple nested `if` statements.

The C# `return` statement immediately terminates execution of the current function and transfers control back to its caller. C# therefore permits a method to contain multiple return points when required by its control flow (Ecma International, 2023). JetBrains’ official C# inspection guidance specifically recommends inverting an `if` statement and returning early when doing so removes unnecessary nesting and makes the method more readable (JetBrains, 2026).

For this project:

- Early returns should be used for **guard conditions**, invalid states, unavailable operations, and legitimate no-operation cases.
- Guard conditions should normally appear near the beginning of the method.
- The normal or successful execution path should remain at the lowest practical indentation level.
- Early returns must still follow the project’s brace rules; braces must not be omitted merely because the body contains only `return`.
- Multiple early returns are acceptable when each represents a clear and independent exit condition.
- Early returns should not be scattered arbitrarily throughout a long method when doing so makes control flow difficult to follow.
- An early return must not bypass required cleanup, persistence, state restoration, or other mandatory operations.
- If cleanup is required regardless of the exit path, constructs such as `using` or `finally` should be used as appropriate.
- Early returns must not be used as a substitute for proper exception handling. Expected no-operation conditions may return normally, while unexpected or exceptional conditions must follow Section 6.1.
- Early returns may reduce **nesting**, but they do not automatically reduce **cyclomatic complexity**. A method containing many independent guard conditions may still violate the complexity limit defined in Section 5.9.

#### Correct

```csharp
public void TryPerformAttack(Player player, Enemy target)
{
    if (!player.IsAlive)
    {
        return;
    }

    if (!player.HasWeapon)
    {
        return;
    }

    if (!target.IsAlive)
    {
        return;
    }

    if (!player.CanReach(target))
    {
        return;
    }

    PerformAttack(player, target);
}
```

The method handles the conditions that prevent an attack first. Once those conditions have been eliminated, the main operation remains at the base indentation level and is immediately visible.

Each early return also represents a legitimate **no-operation state** in the game rather than an unexpected software failure.

#### Incorrect

```csharp
public void TryPerformAttack(Player player, Enemy target)
{
    if (player.IsAlive)
    {
        if (player.HasWeapon)
        {
            if (target.IsAlive)
            {
                if (player.CanReach(target))
                {
                    PerformAttack(player, target);
                }
            }
        }
    }
}
```

The same conditions are represented through four nested levels. The main behavior of the method is buried inside the nested blocks, requiring the reader to keep track of every surrounding condition before reaching the actual attack operation.

Using guard-style early returns makes the normal execution path easier to identify without changing the intended behavior.

#### Incorrect — Uncontrolled Early Return

```csharp
public void CompleteLevel(Player player)
{
    SavePlayerProgress(player);

    if (!player.HasBonusReward)
    {
        return;
    }

    AwardBonusReward(player);

    if (!player.HasCompletedTutorial)
    {
        return;
    }

    UpdateLevelStatistics(player);
    MarkLevelAsCompleted(player);
}
```

Although the method uses early returns, they are placed in the middle of an operation that contains mandatory processing. Depending on the player’s state, `UpdateLevelStatistics` and `MarkLevelAsCompleted` may never execute.

The issue is therefore not the existence of multiple `return` statements, but that the returns allow execution to bypass operations required for completing the level.

A better design would separate optional behavior from mandatory completion logic:

#### Correct — Optional Behavior Without Skipping Required Work

```csharp
public void CompleteLevel(Player player)
{
    SavePlayerProgress(player);

    if (player.HasBonusReward)
    {
        AwardBonusReward(player);
    }

    UpdateLevelStatistics(player);
    MarkLevelAsCompleted(player);
}
```

In this case, an early return is unnecessary because the bonus reward is an optional part of the operation and mandatory level-completion behavior must continue afterward.

---

## 5. C# Programming Practices

### 5.1 Access Modifiers

Access modifiers must be used to control which parts of the program can access a type or member. Project-owned types and members must use the **most restrictive accessibility that still allows the required collaborators to perform their responsibilities**.

Whenever C# permits an explicit access modifier, the project must declare it explicitly instead of relying on the language’s default accessibility. This requirement makes the intended visibility of the code clear during development and code review.

C# defines the following accessibility levels (Ecma International, 2023):

| Accessibility | Meaning | Project Usage |
| --- | --- | --- |
| `private` | Accessible only inside the containing type. | Default choice for implementation details, helper methods, and internal state. |
| `internal` | Accessible only within the current assembly. | Use for types or members shared inside the project assembly but not intended as public API. |
| `protected` | Accessible inside the containing type and derived types. | Use only when a member is intentionally designed as an inheritance extension point. |
| `public` | Accessible without assembly restrictions. | Use only for members intentionally exposed as part of the accessible API. |
| `private protected` | Accessible by the containing type and derived types in the same assembly. | Use only when these specific inheritance and assembly restrictions are required. |
| `protected internal` | Accessible from the current assembly **or** from derived types in other assemblies. | Use only when these broader combined semantics are intentionally required. |

For this project:

- Fields that represent mutable object state must normally be `private`.
- Public or internal access to state should be provided through properties or methods instead of exposing mutable fields directly.
- Helper methods used only by their containing class must be `private`.
- `internal` should be preferred over `public` when a type or member only needs to be used within the same assembly.
- `protected` must only be used when derived classes are intentionally expected to use or override the member.
- `public` must not be used merely because it is convenient to access a member from another class.
- `protected internal` and `private protected` should be used sparingly because their accessibility semantics are more specialized.
- A property may expose a less accessible setter when reading should be allowed more broadly than modification, such as `{ get; private set; }`.
- Accessibility must not be increased merely to make implementation details easier to test.
- Language constructs for which C# does not permit an access modifier are exempt from the explicit-modifier rule.

C# itself provides default accessibility when no modifier is declared. For example, top-level classes default to `internal`, while class members default to `private`. However, this project intentionally requires explicit accessibility where permitted so that developers do not need to remember or infer those defaults (Ecma International, 2023).

#### Correct

```csharp
/// <summary>
/// Manages the health state of the player.
/// </summary>
public sealed class PlayerHealth
{
    private const int InitialHealth = 100;
    private const int MinimumHealth = 0;
    private const int MinimumDamage = 0;

    /// <summary>
    /// Gets the player's current health.
    /// </summary>
    public int CurrentHealth { get; private set; } = InitialHealth;

    /// <summary>
    /// Reduces the player's health by the specified damage.
    /// </summary>
    /// <param name="damage">The amount of damage to apply.</param>
    public void TakeDamage(int damage)
    {
        int normalizedDamage = NormalizeDamage(damage);

        CurrentHealth = Math.Max(
            CurrentHealth - normalizedDamage,
            MinimumHealth);
    }

    private int NormalizeDamage(int damage)
    {
        return Math.Max(damage, MinimumDamage);
    }
}
```

`CurrentHealth` can be read by other parts of the game but can only be modified by `PlayerHealth`. The setter is therefore `private`. `NormalizeDamage` is an implementation detail used only by the class, so it is also `private`. Only the operation that other components need, `TakeDamage`, is exposed publicly.

#### Incorrect

```csharp
/// <summary>
/// Manages the health state of the player.
/// </summary>
public class PlayerHealth
{
    private const int InitialHealth = 100;
    private const int MinimumDamage = 0;

    /// <summary>
    /// Gets or sets the player's current health.
    /// </summary>
    public int CurrentHealth { get; set; } = InitialHealth;

    int NormalizeDamage(int damage)
    {
        return Math.Max(damage, MinimumDamage);
    }
}
```

`CurrentHealth` exposes an unrestricted public setter even though other objects should not be able to modify the player’s health directly. `NormalizeDamage` also relies on C#’s implicit `private` accessibility instead of explicitly declaring its intended access level. Although the code is valid C#, it violates the project’s encapsulation and explicit-access rules.

### 5.2 Auto-Implemented Properties and Property Accessors

Auto-implemented properties should be used when a property only needs to store and expose a value and no custom accessor logic is required. An explicit backing field must not be written only to reproduce the behavior already generated automatically by the compiler.

The property should expose **only the accessors actually required by its contract**:

- `{ get; }` when the value should only be assigned during construction.
- `{ get; init; }` when callers may assign the value only during object initialization.
- `{ get; private set; }` when the containing class may modify the value but external code may only read it.
- `{ get; set; }` only when external callers are intentionally allowed to modify the value.
- A custom accessor should be used when validation, transformation, caching, side effects, or other logic is necessary.

Microsoft states that simple properties requiring no custom accessor code can use auto-implemented properties, while full properties can provide validation, different accessibility, lazy evaluation, or other specialized behavior (Microsoft, 2025k).

#### Correct

```csharp
public class PlayerSettings
{
    public int Score { get; private set; }

    public string PlayerName { get; init; } = string.Empty;

    private int _masterVolume;

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

`Score` and `PlayerName` do not require custom accessor logic, so automatic properties are sufficient. `MasterVolume` requires validation, so a custom setter is justified.

#### Incorrect

```csharp
public class PlayerSettings
{
    private int _score;

    public int Score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
        }
    }
}
```

The explicit backing field and accessor bodies add no behavior beyond what an auto-implemented property already provides. The public setter also exposes unrestricted modification when the game state may need to control score changes internally.

### 5.3 Immutability and `readonly` Usage

Data that is not expected to change after initialization should be modeled as **immutable whenever this behavior correctly represents the game domain**.

For this project, immutability should be preferred for configuration objects, identifiers, definitions, dependencies, and other values whose state should remain stable after initialization. Mutable gameplay state, such as current health, score, ammunition, or position, should remain mutable when changing that state is part of its normal behavior.

C# provides several mechanisms that can be used to restrict unnecessary modification:

- Get-only properties for values that should not change after construction.
- `init` properties for values that may be assigned during object initialization but not afterward.
- `readonly` fields for values assigned during declaration or construction that should not later be reassigned.
- `readonly struct` or immutable record types when appropriate for value-oriented data.

Microsoft states that an `init` accessor allows assignment during object construction while preventing later assignment. Records are also primarily intended to support immutable data models, although they can be designed as mutable when necessary (Microsoft, 2026e, 2026l).

For this project:

- Private fields that are initialized inline or in a constructor and never reassigned must be declared `readonly`.
- Dependencies stored by an object and not replaced during its lifetime should normally be `readonly`.
- Get-only or `init` properties should be preferred when externally supplied data should not change after initialization.
- Mutable properties should only expose setters when modification is part of the object’s intended responsibility.
- `const` should be used for compile-time constants; `readonly` should be used when the value must be determined at runtime or during construction.
- `readonly` must not be interpreted as making a referenced object completely immutable.
- Immutability must not be forced onto game state that is naturally expected to change.

Microsoft’s `IDE0044` code-style rule specifically recommends marking private fields as `readonly` when they are initialized inline or in a constructor and never reassigned. Its default preference is enabled (Microsoft, 2023b).

The `readonly` modifier prevents reassignment of a field after construction. However, when the field contains a reference type, the referenced object itself may still be mutable. In other words, `readonly` protects the **reference stored in the field**, not necessarily all of the object’s internal state (Microsoft, 2026j).

#### Correct — Immutable Data

```csharp
internal sealed class EnemyDefinition
{
    internal string EnemyId { get; }

    internal int MaximumHealth { get; }

    internal EnemyDefinition(
        string enemyId,
        int maximumHealth)
    {
        EnemyId = enemyId;
        MaximumHealth = maximumHealth;
    }
}
```

`EnemyDefinition` represents configuration that should remain stable after creation. Its get-only auto-implemented properties are initialized by the constructor and cannot be reassigned afterward. No explicit backing fields are necessary because the properties do not require custom accessor logic.

#### Correct — `readonly` Dependency

```csharp
internal sealed class EnemySpawner
{
    private readonly IEnemyFactory _enemyFactory;

    internal EnemySpawner(IEnemyFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    internal Enemy SpawnEnemy(EnemyDefinition definition)
    {
        return _enemyFactory.CreateEnemy(definition);
    }
}
```

`_enemyFactory` is a dependency assigned during construction and is not intended to be replaced during the lifetime of `EnemySpawner`. Therefore, the field is declared `readonly`.

#### **Incorrect — Unnecessary Mutability**

```csharp
internal sealed class EnemyDefinition
{
    internal string EnemyId { get; set; }

    internal int MaximumHealth { get; set; }
}
```

The properties can be modified after initialization even though the object represents a stable enemy definition. The setters expose unnecessary mutability.

#### Incorrect — Missing `readonly`

```csharp
internal sealed class EnemySpawner
{
    private IEnemyFactory _enemyFactory;

    internal EnemySpawner(IEnemyFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    internal Enemy SpawnEnemy(EnemyDefinition definition)
    {
        return _enemyFactory.CreateEnemy(definition);
    }
}
```

`_enemyFactory` is assigned during construction and is not intended to be reassigned. Leaving the field mutable does not communicate or enforce that invariant.

#### Correct — Mutable State When Required

```csharp
internal sealed class PlayerHealth
{
    internal int CurrentHealth { get; private set; }

    internal PlayerHealth(int initialHealth)
    {
        CurrentHealth = initialHealth;
    }

    internal void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(
            CurrentHealth - damage,
            0);
    }
}
```

`CurrentHealth` is intentionally mutable because taking damage changes the player’s state during normal gameplay. Immutability should not be applied when modification is part of the object’s responsibility.

#### Important `readonly` Distinction

A `readonly` reference does not necessarily make the referenced object immutable:

```csharp
private readonly List<Enemy> _enemies;
```

After construction, this is not permitted:

```csharp
_enemies = anotherEnemyList;
```

However, this can still be valid:

```csharp
_enemies.Add(enemy);
```

The reference stored in `_enemies` cannot be replaced, but the `List<Enemy>` object itself remains mutable. Microsoft documents this distinction explicitly for reference-type `readonly` fields.

### 5.4 Constants and Magic Values

Gameplay values whose meaning is important to the domain must not be embedded repeatedly as unexplained numeric or string literals. They must instead be represented by a descriptive constant, a `readonly` value, or a configuration value depending on whether the value is compile-time fixed, initialized at runtime, or intended to be adjustable by game design.

Not every literal is a magic value. Values such as `0` or `1` may remain directly in code when their meaning is mathematically or structurally obvious. The rule applies when understanding the value requires knowledge of a gameplay rule or business decision.

Google’s official C# Style Guide explicitly recommends named constants instead of magic numbers and recommends `const` or `readonly` where appropriate (Google, n.d.).

#### Correct

```csharp
private const int MaximumComboMultiplier = 5;

public bool HasMaximumCombo(int comboMultiplier)
{
    return comboMultiplier >= MaximumComboMultiplier;
}
```

`MaximumComboMultiplier` communicates why the value `5` is important and provides a single place to change the rule.

#### Incorrect

```csharp
public bool HasMaximumCombo(int comboMultiplier)
{
    return comboMultiplier >= 5;
}
```

The value `5` has gameplay meaning but the code does not explain what it represents.

### 5.5 Method Responsibility

Classes and methods should have **one cohesive responsibility**. A class should have one primary reason to change, and a method should perform one clearly identifiable operation or coordinate a small set of steps belonging to the same operation.

The project applies the **Single Responsibility Principle (SRP)** to prevent unrelated gameplay, persistence, user-interface, audio, and infrastructure concerns from being concentrated in the same class. Microsoft defines SRP as giving an object one responsibility and one reason to change, which helps produce modular and loosely coupled systems (Microsoft, 2023c).

#### Correct

```csharp
public class PlayerHealth
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

    public void RestoreHealth(int amount)
    {
        CurrentHealth += amount;
    }
}
```

`PlayerHealth` is responsible only for managing player health.

#### Incorrect

```csharp
public class PlayerHealth
{
    public int CurrentHealth { get; private set; }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
    }

    public void SaveGame()
    {
    }

    public void UpdateHealthHud()
    {
    }

    public void PlayHitSound()
    {
    }
}
```

The class mixes health management, persistence, user-interface updates, and audio responsibilities. These concerns can change independently and should therefore be separated.

### 5.6 Avoid Duplicate Code

The same **conceptual behavior** must not be independently implemented in multiple places. When the same rule or calculation is required by several parts of the game, it should be extracted into an appropriate shared method, class, or service.

This rule does not mean that every visually similar fragment must be unified. Two pieces of code that happen to look alike but represent different concepts should remain independent when combining them would create an incorrect abstraction.

Microsoft’s architectural guidance states that duplicated behavior is a frequent source of errors because one copy can be changed while another is forgotten. It also explicitly warns that duplication is preferable to coupling unrelated concepts through the wrong abstraction (Microsoft, 2023c). Visual Studio provides Extract Method specifically as a refactoring that reduces duplication when the same behavior is needed in multiple places (Microsoft, 2026d).

#### Correct

```csharp
private const int MinimumDamage = 1;

private int CalculateDamageAfterArmor(int baseDamage, int armor)
{
    return Math.Max(baseDamage - armor, MinimumDamage);
}

public int CalculatePlayerAttack(int baseDamage, int enemyArmor)
{
    return CalculateDamageAfterArmor(baseDamage, enemyArmor);
}

public int CalculateEnemyAttack(int baseDamage, int playerArmor)
{
    return CalculateDamageAfterArmor(baseDamage, playerArmor);
}
```

The shared damage rule has one authoritative implementation.

#### Incorrect

```csharp
private const int MinimumDamage = 1;

public int CalculatePlayerAttack(int baseDamage, int enemyArmor)
{
    return Math.Max(baseDamage - enemyArmor, MinimumDamage);
}

public int CalculateEnemyAttack(int baseDamage, int playerArmor)
{
    return Math.Max(baseDamage - playerArmor, MinimumDamage);
}
```

The same gameplay rule is duplicated. If the damage formula changes, both implementations must be located and updated independently.

### 5.7 Method Size

Methods should remain **small, focused, and easy to understand**. For this project, a method should normally contain **no more than 30 lines of code**.

Reaching or exceeding 30 lines must be treated as a **refactoring warning**. The developer should review whether the method can be divided into smaller methods with descriptive names without damaging the logical structure of the operation. A method exceeding 30 lines is not automatically incorrect, but it must have a clear justification during code review.

Microsoft-published guidance on source-code readability recommends limiting the vertical size of methods and states that **30 lines should generally be considered a maximum level that triggers an alert and suggests considering refactoring** (Esposito, 2014).

For this project, the 30-line recommendation is measured using the following criteria:

- Count non-blank lines inside the method body.
- Do not count the method declaration.
- Do not count lines containing only opening or closing braces.
- Comments located inside the method count toward its size because they also contribute to the vertical length that developers must read.
- XML documentation placed before the method does not count toward the method size.

Method length must not be evaluated independently. A method should also comply with the project’s rules regarding **Single Responsibility Principle, cyclomatic complexity, duplicated code, and readability**.

#### Correct

```csharp
public void ResolveEnemyDefeat(Enemy enemy)
{
    AwardExperience(enemy);
    SpawnLoot(enemy);
    RemoveEnemyFromLevel(enemy);
    PlayDefeatAnimation(enemy);
}

private void AwardExperience(Enemy enemy)
{
    int experience = enemy.ExperienceReward;
    _playerProgress.AddExperience(experience);
}

private void SpawnLoot(Enemy enemy)
{
    if (enemy.LootTable != null)
    {
        _lootSpawner.Spawn(enemy.LootTable, enemy.Position);
    }
}

private void RemoveEnemyFromLevel(Enemy enemy)
{
    _enemyManager.Remove(enemy);
}

private void PlayDefeatAnimation(Enemy enemy)
{
    enemy.Animator.Play(DefeatAnimationName);
}
```

Each method remains well below the recommended 30-line maximum and represents a clearly named operation. The higher-level method coordinates the enemy-defeat process without containing all implementation details itself.

#### Incorrect

```csharp
public void ResolveEnemyDefeat(Enemy enemy)
{
    int experience = enemy.ExperienceReward;
    int experienceMultiplier = enemy.MultiplierLevel;
    _playerProgress.AddExperience(experience * experienceMultiplier);

    if (enemy.LootTable != null)
    {
        _lootSpawner.Spawn(
            enemy.LootTable,
            enemy.Position);
    }

    _enemyManager.Remove(enemy);

    enemy.Animator.Play("Defeat");

    if (enemy.IsBoss)
    {
        _gameState.BossDefeated = true;
        _musicManager.StopBossMusic();
        _musicManager.PlayExplorationMusic();
        _doorManager.UnlockBossDoors();
        _achievementManager.Unlock("BossDefeated");

        if (_playerProgress.CurrentLevel < MaximumPlayerLevel)
        {
            int bonusExperience = enemy.ExperienceReward
                * BossExperienceMultiplier;

            _playerProgress.AddExperience(
                bonusExperience);
        }
    }

    if (_enemyManager.RemainingEnemies == 0)
    {
        _gameState.LevelCompleted = true;
        _hud.ShowLevelCompleteMessage();
        _checkpointManager.SaveCheckpoint();
        _audioManager.PlayLevelCompleteSound();
    }

    _hud.UpdateExperienceBar();
    _hud.UpdateEnemyCounter();
    _statistics.RegisterEnemyDefeated(enemy);
    _logger.LogInformation(
        "Enemy {EnemyId} was defeated.",
        enemy.Id);
}
```

The method exceeds the project’s recommended 30-line maximum and contains several independent operations related to experience, loot, animation, boss-state management, music, doors, achievements, level completion, persistence, UI, statistics, and logging.

The method should be reviewed and divided into cohesive operations such as `AwardExperience`, `HandleBossDefeat`, `HandleLevelCompletion`, and `RegisterEnemyDefeat`. The problem is not only its physical size: its length also reveals multiple responsibilities that make the method harder to understand, test, and maintain.

### 5.8 Parameter Count

Project-owned methods and constructors must declare a maximum of three parameters. Calls to methods provided by the .NET runtime, the selected game engine, or third-party libraries are not subject to this limit.

When a method requires more than three pieces of input, the developer must review its design before adding additional parameters. Related values should normally be grouped into a meaningful class, record, or struct when they represent one conceptual input. If the parameters represent unrelated concerns, the method should instead be reviewed for multiple responsibilities and divided into smaller cohesive operations.

The limit of three parameters is a **project-specific maintainability rule**, not a requirement of the C# language or the .NET runtime. GitLab’s published engineering style guide uses the same threshold, requiring an object when a function or method would contain more than three parameters. Google’s official C# Style Guide similarly recommends grouping multiple configuration options into a class or struct because doing so reduces argument count and makes method calls easier to read and write (GitLab, n.d.; Google, n.d.).

For this project:

- Zero to three parameters are permitted.
- Four or more parameters violate the standard unless an explicitly documented exception has been approved.
- Constructors may accept a fourth parameter exclusively when the additional parameter is `ILogger<T>` representing a cross-cutting infrastructure logging dependency as required by Section 6.3.
- A `params` parameter must not be used merely to bypass the three-parameter rule.
- Related parameters should be grouped only when they form a meaningful domain concept.
- Unrelated values must not be placed into an artificial parameter object solely to satisfy the numeric limit.
- Multiple Boolean parameters should be avoided; when they represent mutually related options, an `enum` or configuration type should be considered.
- A long parameter list may indicate that the method has more than one responsibility and should therefore also be reviewed against Section 5.5, **Method Responsibility**.

#### Correct

```csharp
public void SpawnEnemy(
    EnemyType enemyType,
    Vector3 position,
    int level)
{
    // Enemy spawning logic.
}
```

The method contains three clearly related parameters and remains within the project’s maximum.

#### Correct — Cross-Cutting Logger Exception

```cpp
public sealed class LoginHandler
{
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IAccountLoginStore accountStore,
        IPasswordHasher passwordHasher,
        IAuthenticatedSessionStore sessionStore,
        ILogger<LoginHandler> logger)
    {
        _logger = logger;
        // Remaining dependencies assigned here.
    }
}
```

The constructor accepts three application dependencies plus `ILogger<T>` as an authorized fourth parameter for cross-cutting infrastructure logging.

#### Correct — Parameter Object

When additional related configuration is necessary, the values can be represented by a meaningful domain type:

```csharp
public sealed class EnemySpawnOptions
{
    public EnemyType EnemyType { get; init; }

    public Vector3 Position { get; init; }

    public int Level { get; init; }

    public bool IsElite { get; init; }

    public float HealthMultiplier { get; init; }
}

public void SpawnEnemy(EnemySpawnOptions options)
{
    // Enemy spawning logic.
}
```

`EnemySpawnOptions` represents one coherent concept: the configuration required to spawn an enemy. The method receives one meaningful parameter instead of an increasingly difficult positional argument list.

#### Incorrect

```csharp
public void SpawnEnemy(
    EnemyType enemyType,
    Vector3 position,
    int level,
    bool isElite,
    float healthMultiplier)
{
    // Enemy spawning logic.
}
```

The method contains five parameters and therefore exceeds the project’s maximum of three. As the parameter list grows, callers must remember the meaning and position of more arguments, and the method signature becomes more difficult to read and modify.

#### Incorrect — Artificial Parameter Grouping

```csharp
public sealed class GameMethodParameters
{
    public Enemy Enemy { get; init; }

    public int Damage { get; init; }

    public AudioClip VictoryMusic { get; init; }

    public string SaveFilePath { get; init; }
}

public void ProcessGame(GameMethodParameters parameters)
{
    DamageEnemy(parameters.Enemy, parameters.Damage);
    PlayMusic(parameters.VictoryMusic);
    SaveGame(parameters.SaveFilePath);
}
```

Although the method technically receives only one parameter, the parameter object combines unrelated combat, audio, and persistence concerns. This does not solve the design problem; it hides multiple responsibilities behind a single parameter.

The method should instead be divided into cohesive operations.

### 5.9 Cyclomatic Complexity

A method should have a cyclomatic complexity of **10 or lower**. When a method exceeds this threshold, it must be reviewed and normally refactored into smaller cohesive operations unless the team documents a justified exception.

Microsoft explains that higher cyclomatic complexity makes code harder to understand, test, maintain, and troubleshoot. Microsoft also cites NIST guidance stating that a value of **10 is a good starting limit**, while acknowledging that no single threshold fits every organization (Microsoft, 2025e). The .NET analyzer `CA1502` has a configurable threshold and Microsoft’s documentation even demonstrates configuring it to `10` (Microsoft, 2023d).

This means that **10 is a project threshold supported by published Microsoft/NIST guidance**, not a rule imposed by the C# language.

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

The method contains few decision paths and delegates the remaining decision process to another cohesive operation.

#### Incorrect

```csharp
public int CalculateEnemyPriority(Enemy enemy)
{
    int priority = 0;

    if (enemy.IsAlive)
    {
        priority++;
    }

    if (enemy.IsVisible)
    {
        priority++;
    }

    if (enemy.HasTarget)
    {
        priority++;
    }

    if (enemy.CanSeePlayer)
    {
        priority++;
    }

    if (enemy.IsInAttackRange)
    {
        priority++;
    }

    if (enemy.HasEnoughStamina)
    {
        priority++;
    }

    if (enemy.IsBoss)
    {
        priority++;
    }

    if (enemy.IsEnraged)
    {
        priority++;
    }

    if (enemy.IsLowHealth)
    {
        priority++;
    }

    if (enemy.CanUseSpecialAttack)
    {
        priority++;
    }

    return priority;
}
```

The ten independent `if` branches plus the base path produce a cyclomatic complexity above the project’s limit. The decision rules should be reorganized or divided into meaningful operations.

### 5.10 Nullable Reference Types

Nullable reference type analysis must be enabled for the project. Reference types that are intended never to contain `null` must be declared as non-nullable, while `?` must only be used when absence is an intentional part of the domain or external contract.

Nullable warnings must not be silenced without justification. The null-forgiving operator (`!`) should not be used merely to make a compiler warning disappear.

Microsoft describes nullable reference types as a compile-time mechanism for expressing design intent and detecting cases where code does not match that intent. Microsoft also recommends using the null-forgiving operator sparingly because it disables part of the compiler’s protection (Microsoft, 2026i).

#### Correct

```csharp
public class PlayerProfile
{
    public string PlayerName { get; }

    public PlayerProfile(string playerName)
    {
        PlayerName = playerName;
    }
}
```

The class declares and maintains a non-null contract for `PlayerName`.

#### Incorrect

```csharp
public class PlayerProfile
{
    public string PlayerName { get; set; } = null!;
}
```

The null-forgiving operator suppresses the warning without establishing a real non-null invariant.

### 5.11 Null Handling

**Project-owned methods must not return `null` as a sentinel value.** A method’s return contract should allow callers to use the result without first guessing whether `null` means “not found,” “empty,” “failed,” or “not initialized.”

The project must use the following alternatives:

- Return an empty collection when a collection contains no results.
- Return a valid default/domain object only when that value has a legitimate domain meaning.
- Use a `Try...` pattern when absence is an expected outcome of a lookup.
- Use a dedicated result type when an operation needs to communicate success, failure, or additional information.
- Throw an appropriate exception only for exceptional conditions, following Section 6.1.
- If an external framework API requires nullable returns, isolate that nullable contract and convert it to the project’s chosen contract at the project boundary.

This is intentionally **stricter than the C# language itself**. Microsoft supports nullable return types such as `string?` when `null` is part of the API contract. However, Microsoft also demonstrates returning non-null fallbacks such as `Enumerable.Empty<T>()` so callers do not require null checks. The project adopts the latter approach for project-owned APIs to make beginner code contracts more explicit (Microsoft, 2026i, 2026n).

#### Correct

```csharp
public IReadOnlyList<Enemy> GetDefeatedEnemies()
{
    List<Enemy> defeatedEnemies = new List<Enemy>();

    foreach (Enemy enemy in _enemies)
    {
        if (!enemy.IsAlive)
        {
            defeatedEnemies.Add(enemy);
        }
    }

    return defeatedEnemies;
}
```

If no defeated enemies exist, the method returns an empty collection rather than `null`.

#### Incorrect

```csharp
public IReadOnlyList<Enemy> GetDefeatedEnemies()
{
    if (_enemies.Count == 0)
    {
        return null;
    }

    return _enemies;
}
```

The caller must now determine whether `null` represents no enemies, an error, or another state, and the declared non-nullable return contract is violated.

### 5.12 Pattern Matching

Pattern matching should be used when code needs to verify the runtime type or simple structure of a value and immediately use the matched value.

For this project, declaration patterns such as `value is Type variable` and negative patterns such as `value is not Type variable` should be preferred over performing a separate type test followed by a cast, or an `as` conversion followed by a null check.

C# pattern matching allows an expression to be tested against a pattern using constructs such as the `is` operator and `switch`. A declaration pattern can test whether a value is compatible with a specified type and, when the match succeeds, introduce a variable of that type without requiring a separate cast (Ecma International, 2023).

Microsoft's code-style rules `IDE0019`, `IDE0020`, and `IDE0038` explicitly support preferring pattern matching over `as` followed by a null check or an `is` check followed by an explicit cast (Microsoft, 2023k; Microsoft, 2024).

For this project:

- Use a declaration pattern when the code needs both to verify a runtime type and use the resulting typed value.
- Prefer `is not` with an early return when a method cannot continue unless a value has the expected type.
- Pattern matching is especially appropriate in WPF event handlers where `sender`, `Tag`, `DataContext`, or selected items must be checked before use.
- Avoid repeating a type check and a cast when a declaration pattern can perform both operations.
- Avoid `as` followed by a null check when a declaration pattern communicates the same intent more directly.
- Patterns must remain simple enough to understand without mentally decomposing several nested conditions.
- Complex domain behavior must not be compressed into deeply nested property, relational, or logical patterns merely to reduce the number of lines.
- When a pattern condition becomes difficult to describe at a glance, extract the decision into a clearly named method.
- The `var` pattern must not be used in project-owned code because Section 5.18 requires explicit local variable types.
- Pattern matching only verifies the characteristics represented by the pattern. It must not be treated as a replacement for input validation, authorization, or other domain validation.
- Pattern features must be supported by the language version configured by the project.

#### Correct — Type Test and Capture

```csharp
private void OnSquareMouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs eventArgs)
{
    if (sender is Border border
        && border.Tag is BoardPosition position)
    {
        SquareSelected?.Invoke(
            this,
            new BoardSquareSelectedEventArgs(position));
    }
}
```

The declaration patterns verify that `sender` is a `Border` and that its `Tag` contains a `BoardPosition`. The matched values are captured directly as strongly typed variables, so no additional casts are required.

#### Correct — Negative Pattern with Early Return

```csharp
private void OnJoinMatchClick(
    object sender,
    RoutedEventArgs eventArgs)
{
    if (sender is not Button button
        || button.Tag is not string matchId
        || button.DataContext is not AvailableMatchItem item)
    {
        return;
    }

    OpenJoinConfirmation(matchId, item);
}
```

The method cannot continue unless all three values have the expected types. Using negative patterns together with an early return keeps the valid execution path at the lowest indentation level and is consistent with Section 4.10, **Early Returns**.

#### **Incorrect**

```csharp
private void OnJoinMatchClick(
    object sender,
    RoutedEventArgs eventArgs)
{
    Button? button = sender as Button;

    if (button != null)
    {
        string? matchId = button.Tag as string;
        AvailableMatchItem? item =
            button.DataContext as AvailableMatchItem;

        if (matchId != null && item != null)
        {
            OpenJoinConfirmation(matchId, item);
        }
    }
}
```

The code is valid C#, but it performs separate `as` conversions and null checks for values that can be tested and captured directly with declaration patterns. It also introduces additional nesting that makes the successful execution path less visible.

Microsoft's `IDE0019` rule specifically identifies this `as`-followed-by-null-check structure as a case where pattern matching can be preferred (Microsoft, 2023k).

#### Incorrect — Repeated Type Test and Cast

```csharp
private void OpenFriendProfile(object sender)
{
    if (sender is Button)
    {
        Button button = (Button)sender;

        if (button.Tag is string)
        {
            string username = (string)button.Tag;
            NavigateToProfile(username);
        }
    }
}
```

The code first verifies each runtime type and then performs an explicit cast of the same value. Declaration patterns can perform the type test and variable assignment together, avoiding duplication and making the intended control flow clearer.

Microsoft's `IDE0020` and `IDE0038` rules specifically support pattern matching instead of an `is` check followed by a cast (Microsoft, 2024).

### 5.13 LINQ Usage

LINQ should be used when it makes collection querying or transformation clearer and more declarative than an equivalent manual loop.

LINQ is appropriate for operations such as filtering, projection, ordering, grouping, aggregation, searching, and determining whether elements satisfy a condition. Common operators include `Where`, `Select`, `OrderBy`, `ThenBy`, `GroupBy`,`Any`, `All`, `Count`, `Sum`, `FirstOrDefault`, `Take`, `ToArray`, and `ToList`.

Microsoft describes LINQ as a consistent C# model for querying and transforming data from sources such as in-memory collections and databases. LINQ to Objects can make filtering, ordering, and grouping operations more concise and readable than equivalent manual iteration (Microsoft, 2025m).

For this project:

- LINQ should be preferred when the operation represents a clear query over a collection.
- Method syntax such as `.Where(...)`, `.Select(...)`, and `.OrderBy(...)` should be preferred for consistency.
- Predicates and selectors used by LINQ must remain short and easy to understand.
- Complex predicates or transformations should be extracted into clearly named methods.
- LINQ predicates and selectors should not perform unrelated side effects or modify application state.
- Lambda expressions used inside LINQ must also follow Section 5.14, Lambda Expressions.
- Query chains should be formatted so that each major operation is easy to identify.
- A query should not be split into many unnecessary intermediate collections when one readable query can express the operation.
- Developers must understand whether a LINQ operation uses deferred or immediate execution.
- `ToArray()` or `ToList()`should be used when the project requires a stable snapshot of the query results or when deferred execution would allow the source collection to change before enumeration.
- Queries created from a mutable collection while holding a synchronization lock must be materialized before leaving the protected block when the returned result is intended to represent the state observed inside that block.
- Repeated enumeration of the same expensive query should be avoided. Materialize the result when it must be reused without reevaluating the source.
- In Entity Framework queries, filtering, grouping, and projection should normally be applied before materializing the result so that the database provider can execute those operations.
- Read-only Entity Framework queries should use `AsNoTracking()` when the returned entities do not need to be updated through the current context.
- The project must not use `var` for LINQ query results because Section 5.18 requires explicit local variable types.

Microsoft distinguishes between deferred execution, where a query is evaluated when its results are enumerated, and immediate execution, used by operations such as `Count`, `First`, `ToList`, and `ToArray`. Calling `ToList()` or `ToArray()` evaluates the query and stores its current results in memory (Microsoft, 2025m).

Entity Framework `DbSet` objects implement `IQueryable`, so LINQ expressions can be translated into database queries. Operations such as enumeration, `ToList`, `ToArray`, and `First` cause the query to execute against the database (Microsoft, 2020b). For read-only Entity Framework queries, Microsoft documents `AsNoTracking()` as the mechanism for retrieving entities without adding them to the context's change tracker (Microsoft, 2020a).

#### Correct  — Filtering, Ordering, and Materialization

```csharp
internal IReadOnlyList<FriendshipRequest> GetPendingReceived(
    string recipientUsername)
{
    lock (_syncRoot)
    {
        return _friendRequests
            .Where(request =>
                request.State == FriendshipRequestState.Pending)
            .Where(request => string.Equals(
                request.RecipientUsername,
                recipientUsername,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToArray();
    }
}
```

The query clearly describes the required collection operation: select pending requests for one recipient, order them by creation time, and return the result.

`ToArray()` also materializes the query before leaving the synchronization block. The returned collection therefore represents a stable snapshot of the state observed while the lock was held.

#### Incorrect — Deferred Query Escaping a Protected Block

```csharp
internal IEnumerable<FriendshipRequest> GetPendingReceived(
    string recipientUsername)
{
    lock (_syncRoot)
    {
        return _friendRequests
            .Where(request =>
                request.State == FriendshipRequestState.Pending)
            .Where(request => string.Equals(
                request.RecipientUsername,
                recipientUsername,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(request => request.CreatedAtUtc);
    }
}
```

The query is returned without being executed. Because most LINQ operators over `IEnumerable<T>` use deferred execution, enumeration may occur after the method has left the `lock`.

The underlying collection may therefore change before or during enumeration. When the method intends to return the state observed inside the synchronized operation, the query must be materialized before leaving the protected block.

#### Correct — Entity Framework Query

```csharp
private static Task<List<LeaderboardPlayer>> LoadPlayersAsync(
    ReversiGameSystemEntities context)
{
    return context.Account
        .AsNoTracking()
        .Where(account =>
            account.statusCode == ActiveAccountStatusCode)
        .Select(account => new LeaderboardPlayer
        {
            AccountId = account.accountId,
            Username = account.username,
            ProfileImageUri = account.profileImageUri
        })
        .ToListAsync();
}
```

The query filters the database rows and projects only the values required by the leaderboard before materializing the result. `AsNoTracking()` is appropriate because the retrieved account information is used only for reading.

#### Incorrect — Premature Database Materialization

```csharp
private static async Task<List<LeaderboardPlayer>> LoadPlayersAsync(
    ReversiGameSystemEntities context)
{
    List<Account> accounts = await context.Account.ToListAsync();

    return accounts
        .Where(account =>
            account.statusCode == ActiveAccountStatusCode)
        .Select(account => new LeaderboardPlayer
        {
            AccountId = account.accountId,
            Username = account.username,
            ProfileImageUri = account.profileImageUri
        })
        .ToList();
}
```

The complete account table is materialized before the filter and projection are applied. The remaining LINQ operations therefore execute against the in-memory collection instead of being part of the database query.
When the operations can be represented by the Entity Framework query provider, filtering and projection should occur before materialization.

#### Incorrect — Side Effects Inside a Query

```csharp
IEnumerable<FriendCandidate> activeAccounts =
    accounts.Where(account =>
    {
        RegisterAccountSearch(account);
        return account.IsActive;
    });
```

The predicate performs an unrelated state-changing operation while deciding whether the element belongs in the result.

LINQ predicates should describe the query condition. Side effects such as logging domain events, changing game state, storing data, or registering activity should normally be performed separately from the query so that enumeration does not unexpectedly modify the application.

If the predicate requires substantial logic, it should instead be represented by a named method:

```csharp
IEnumerable<FriendCandidate> activeAccounts =
    accounts.Where(IsAvailableFriendCandidate);
```

This also follows Section 5.14 when the required lambda would otherwise become non-trivial.

### 5.14 Lambda Expressions

Lambda expressions are permitted when they represent **short, local, and immediately understandable behavior**.

They may be used for:

- Simple LINQ predicates.
- Simple LINQ projections.
- Short callbacks passed directly to another method.
- Short one-use delegate operations.
- Event handlers that do not need to be unsubscribed later.
- Simple expressions where creating a separately named method would make the code less clear.

A lambda should normally contain one expression or no more than **two or three simple statements**. A named method should be used when the behavior:

- Is reused in multiple locations.
- Requires several branches.
- Contains loops.
- Contains exception handling.
- Performs several side effects.
- Becomes difficult to describe at a glance.
- Exceeds approximately two or three meaningful statements.

Microsoft’s C# reference states that statement lambdas may technically contain any number of statements, but in practice they typically contain no more than two or three. Microsoft also documents lambdas as appropriate for small pieces of behavior and for event handlers that do not need later removal. Google’s official C# standard similarly recommends a named method when a lambda is non-trivial or reused (Google, n.d.; Microsoft, 2025f, 2026f).

#### Correct

```csharp
IEnumerable<Enemy> activeEnemies =
    enemies.Where(enemy => enemy.IsAlive);
```

The predicate is short, local, has no side effects, and its purpose is immediately clear.

#### Incorrect

```csharp
IEnumerable<Enemy> attackableEnemies = enemies.Where(enemy =>
{
    UpdateEnemyDetection(enemy);

    if (!enemy.IsAlive)
    {
        return false;
    }

    if (enemy.IsStunned)
    {
        return false;
    }

    if (!enemy.CanSeePlayer)
    {
        return false;
    }

    RegisterEnemyTarget(enemy);

    return enemy.IsInAttackRange;
});
```

The lambda performs state changes and several decisions. This behavior should be extracted into one or more named methods so that its purpose and side effects are explicit.

### 5.15 Events and Delegates

Events should be used when one component needs to notify zero or more subscribers that something has occurred without depending on a response from those subscribers.

Delegates should be used when code needs to represent a callable operation, callback, strategy, or function that is supplied by another component.

.NET events are based on delegates. An event publisher raises a notification, while subscribers decide how to respond. The publisher does not need to know which objects are subscribed to the event. .NET provides `EventHandler` and `EventHandler<TEventArgs>` for most event scenarios (Microsoft, 2026p).

For this project:

- Use an event when a component is announcing that something has happened and the publisher can continue even when no subscribers exist.
- Prefer `EventHandler` when an event carries no custom data.
- Prefer `EventHandler<TEventArgs>` when an event needs to provide data to subscribers.
- Custom event-data classes must derive from `EventArgs` and follow the `EventArgs` suffix convention defined in Section 3.16.
- Event names must follow Section 3.13, Events.
- Events should normally be raised by the class that declares them.
- Use the null-conditional invocation form `EventName?.Invoke(...)` when subscribers are optional.
- The object that raises an instance event should normally pass `this` as the sender.
- Event handlers should not return values. If the calling code requires a return value from another operation, use a delegate or another explicit abstraction instead of an event.
- Use `Action` or `Action<T>` for simple callbacks that do not return a value.

`SquareSelected` represents a notification that can have zero or more subscribers. The publisher does not require a response from those subscribers, so an event is appropriate.

The event uses `EventHandler<TEventArgs>`, passes the publisher as `sender`, and places its event-specific information in a dedicated `EventArgs` type.

#### Incorrect — Public Delegate Used as an Event

```csharp
internal sealed class ReversiBoardControl
{
    internal Action<BoardPosition>? SquareSelected;

    private void RaiseSquareSelected(BoardPosition position)
    {
        SquareSelected?.Invoke(position);
    }
}
```

Although the delegate can technically notify other code, it exposes a delegate field instead of expressing the notification as an event.

Code with access to the member can replace the delegate value instead of only subscribing or unsubscribing. An `event` communicates the intended publisher/subscriber relationship and restricts external code to adding or removing handlers.

#### Correct — Delegate Callback

```csharp
internal sealed class LocalizedTextProvider
{
    private readonly Func<string, string> _resourceLookup;

    internal LocalizedTextProvider(
        Func<string, string> resourceLookup)
    {
        _resourceLookup = resourceLookup;
    }

    internal string GetText(string resourceKey)
    {
        return _resourceLookup(resourceKey);
    }
}
```

The resource lookup is not a notification. `LocalizedTextProvider` requires another operation to receive a key and return a localized string.

A `Func<string, string>` therefore represents the dependency more accurately than an event.

#### Incorrect — Event Used for a Required Return Value

```csharp
internal sealed class LocalizedTextProvider
{
    internal event Func<string, string>? ResourceRequested;

    internal string GetText(string resourceKey)
    {
        if (ResourceRequested == null)
        {
            return string.Empty;
        }

        return ResourceRequested(resourceKey);
    }
}
```

The operation requires another component to produce a value before `GetText` can complete correctly. This is callback behavior rather than event-notification behavior.

Using an event also creates ambiguity if several subscribers are registered because multicast delegates may contain multiple handlers while the caller expects one meaningful return value.

#### Correct — Subscription with Explicit Cleanup

```csharp
internal sealed class GamePage
{
    private readonly ReversiBoardControl _boardControl;

    internal GamePage(ReversiBoardControl boardControl)
    {
        _boardControl = boardControl;

        _boardControl.SquareSelected += HandleSquareSelected;
    }

    internal void Detach()
    {
        _boardControl.SquareSelected -= HandleSquareSelected;
    }

    private void HandleSquareSelected(
        object? sender,
        BoardSquareSelectedEventArgs eventArgs)
    {
        SelectSquare(eventArgs.Position);
    }

    private void SelectSquare(BoardPosition position)
    {
        // Selection logic.
    }
}
```

A named handler can be subscribed and later removed using the same method reference. This is appropriate when the publisher may continue to exist after the subscriber no longer needs notifications.

#### Incorrect — Anonymous Subscription That Cannot Be Easily Removed

```csharp
internal sealed class GamePage
{
    private readonly ReversiBoardControl _boardControl;

    internal GamePage(ReversiBoardControl boardControl)
    {
        _boardControl = boardControl;

        _boardControl.SquareSelected +=
            (sender, eventArgs) =>
            {
                SelectSquare(eventArgs.Position);
            };
    }

    private void SelectSquare(BoardPosition position)
    {
        // Selection logic.
    }
}
```

The subscription uses an anonymous lambda without storing the delegate instance. If the page later needs to unsubscribe, it cannot simply create another identical lambda because that would be a different delegate instance.
When an event subscription has a shorter lifetime than its publisher, a named handler or stored delegate should be used instead.

### 5.16 Resource Disposal

Objects that hold resources and implement `IDisposable` must be disposed when the code that owns them no longer needs them.

Project-owned code should use a `using` statement or `using` declaration whenever the lifetime of an `IDisposable` object can be represented directly by a local scope. This includes resources such as file streams, memory streams, cryptographic objects, image objects, and Entity Framework database contexts.

The C# `using` statement guarantees that the acquired disposable object is disposed when execution leaves the `using` scope, including when the block exits through an exception or a `return` statement. A `using` declaration provides the same deterministic disposal behavior, but the resource remains available until the end of the scope in which it was declared (Microsoft, 2026q).

For this project:

- A locally created object that implements `IDisposable` and remains owned by the current operation must be disposed before the operation loses access to it.
- Prefer a `using` statement when a resource is needed only for a limited part of a method and an explicit block makes its lifetime easier to identify.
- A `using` declaration may be used when the resource is naturally required until the end of the current method or block.
- Disposable resources should have the shortest practical lifetime.
- Do not rely on the garbage collector to release disposable resources at an appropriate time.
- Do not manually call `Dispose()` when a `using` statement or declaration can express the same lifetime more safely and clearly.
- Code must not access a disposable object after its lifetime has ended.
- When multiple disposable objects are created for one operation, each owned resource must be disposed.
- When one disposable object depends on another, their scopes must make their ownership and required lifetime clear.
- A method that transfers ownership of a disposable resource to another object or to its caller must not dispose that resource before the new owner can use it. Ownership transfer should be clear from the API design.
- Entity Framework contexts created inside project persistence operations must be scoped to the operation and disposed when that operation finishes.
- Early returns must not bypass required resource cleanup. When a disposable resource is declared through `using`, C# guarantees disposal before the method actually returns.
- Exceptions must not cause locally owned disposable resources to remain undisposed.

Microsoft's reliability rule `CA2000` reports situations where a local `IDisposable` object is created but may leave scope without being disposed. The rule also recognizes that disposal ownership may sometimes be transferred to another object or caller instead of remaining with the method that originally created the resource (Microsoft, 2025n).

Entity Framework 6 documentation similarly states that the lifetime of a `DbContext` ends when it is disposed or garbage-collected and recommends `using` when application code owns the context and wants the resources controlled by it to be released when the operation ends (Microsoft, 2020c).

#### Correct — Limited File Resource Lifetime

```csharp
private static bool HasSupportedImageContent(FileInfo file)
{
    using (FileStream stream = file.OpenRead())
    {
        BitmapDecoder decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.None,
            BitmapCacheOption.OnDemand);

        if (!(decoder is PngBitmapDecoder
            || decoder is JpegBitmapDecoder)
            || decoder.Frames.Count == 0)
        {
            return false;
        }

        BitmapFrame frame = decoder.Frames[0];

        return frame.PixelWidth >= 1
            && frame.PixelHeight >= 1
            && frame.PixelWidth <= MaximumDimension
            && frame.PixelHeight <= MaximumDimension;
    }
}
```

The `FileStream` is created and owned by the method, so its lifetime is explicitly limited by the `using` statement.

The method contains early `return` statements, but they do not bypass resource cleanup because C# disposes the stream when execution leaves the `using` block.

#### **Incorrect — Manual Cleanup That Can Be Bypassed**

```csharp
private static bool HasSupportedImageContent(FileInfo file)
{
    FileStream stream = file.OpenRead();

    BitmapDecoder decoder = BitmapDecoder.Create(
        stream,
        BitmapCreateOptions.None,
        BitmapCacheOption.OnDemand);

    if (decoder.Frames.Count == 0)
    {
        return false;
    }

    BitmapFrame frame = decoder.Frames[0];

    stream.Dispose();

    return frame.PixelWidth >= 1
        && frame.PixelHeight >= 1;
}
```

If `decoder.Frames.Count` is zero, the method returns before `Dispose()` is reached. An exception occurring before the explicit call could produce the same problem.

The resource lifetime should instead be represented by `using`, which guarantees cleanup when control leaves the protected scope.

#### Correct — Multiple Disposable Resources

```csharp
private static bool HasSupportedDimensions(byte[] content)
{
    using (MemoryStream stream =
        new MemoryStream(content, writable: false))
    using (Image image = Image.FromStream(
        stream,
        useEmbeddedColorManagement: false,
        validateImageData: true))
    {
        return image.Width >= 1
            && image.Height >= 1
            && image.Width <= ProfileImagePolicy.MaximumDimension
            && image.Height <= ProfileImagePolicy.MaximumDimension;
    }
}
```

Both `MemoryStream` and `Image` are disposable resources owned by the operation.

Their lifetimes are represented explicitly, and both resources are disposed when execution leaves the block.

#### Correct — Entity Framework Context

```csharp
public async Task<LeaderboardStoreResult> GetAsync(
    int maximumEntries)
{
    using (ReversiGameSystemEntities context =
        new ReversiGameSystemEntities())
    {
        List<LeaderboardPlayer> players =
            await LoadPlayersAsync(context);

        List<PlayerResultCount> counts =
            await LoadResultCountsAsync(context);

        List<RankedLeaderboardPlayer> rankedPlayers =
            RankPlayers(
                players,
                counts,
                maximumEntries);

        List<LeaderboardEntry> entries =
            CreateEntries(rankedPlayers);

        return new LeaderboardStoreResult(
            LeaderboardStoreOutcome.Success,
            entries);
    }
}
```

The persistence operation creates the `ReversiGameSystemEntities` context and therefore owns its lifetime.

The context remains available while the database operations are executed and is disposed before the method completes.

#### **Incorrect — Context Left Undisposed**

```csharp
public async Task<LeaderboardStoreResult> GetAsync(
    int maximumEntries)
{
    ReversiGameSystemEntities context =
        new ReversiGameSystemEntities();

    List<LeaderboardPlayer> players =
        await LoadPlayersAsync(context);

    List<PlayerResultCount> counts =
        await LoadResultCountsAsync(context);

    List<RankedLeaderboardPlayer> rankedPlayers =
        RankPlayers(
            players,
            counts,
            maximumEntries);

    List<LeaderboardEntry> entries =
        CreateEntries(rankedPlayers);

    return new LeaderboardStoreResult(
        LeaderboardStoreOutcome.Success,
        entries);
}
```

The method creates the database context but never deterministically disposes it.

Because the operation owns the context, its lifetime should be represented by a `using` statement instead of depending on garbage collection.

#### Correct — Using Declaration

```csharp
private static TContract RoundTrip<TContract>(
    TContract value)
{
    DataContractSerializer serializer =
        new DataContractSerializer(typeof(TContract));

    using MemoryStream stream = new MemoryStream();

    serializer.WriteObject(stream, value);
    stream.Position = 0;

    object? result = serializer.ReadObject(stream);
    object nonNullResult = TestAssert.IsNotNull(result);

    return (TContract)nonNullResult;
}
```

The `MemoryStream` is required until the method finishes, so a `using` declaration clearly expresses that its lifetime extends through the remainder of the current scope.

Both `using` statements and `using` declarations are acceptable in this project. The choice should communicate the intended resource lifetime rather than being treated only as a formatting preference.

### 5.17 Serialization

Serialized data exchanged between project components must use explicit and stable data contracts.

For the project's service boundary, serializable request, response, and DTO types must be defined in a specific project instead of exposing domain objects, persistence entities, or user-interface types directly.

A data contract describes the data exchanged between components independently of the concrete implementation type. WCF uses `DataContractSerializer`by default, and Microsoft supports explicitly defining serialized types through `DataContractAttribute` and serialized members through `DataMemberAttribute` (Microsoft, 2021e).

For this project:
- Complex serialized contract types must explicitly use `[DataContract]`.
- Project data contracts must use `ContractNamespaces.Data` as their serialization namespace.
- Properties that form part of the serialized representation must explicitly use `[DataMember]`.
- Every `DataMember` must define an explicit and stable `Order`.
- Existing `DataMember` order values must not be renumbered after the contract has been established.
- New members added to an existing contract should use new order values after the existing members instead of reorganizing the previous order.
- Members that are required by the contract may use `IsRequired = true`.
- Adding a new required member to an already existing contract must be treated as a potentially breaking change and reviewed before implementation.
- Optional nullable members may use `EmitDefaultValue = false` when omission of their default value is intentionally part of the contract.
- `IsRequired = true` and `EmitDefaultValue = false` must not be combined casually because the combination can produce compatibility and round-trip problems.
- Enumerations marked with `[DataContract]` must explicitly mark every serialized value with `[EnumMember]`.
- Data-contract namespaces must remain stable. Changing the contract name, namespace, member order, or serialized member type must be treated as a compatibility-sensitive change.
- Persistence entities, WPF controls, pages, and other implementation-specific types must not be used directly as service data contracts.
- Serialized contract types should primarily contain transport data and should not contain unrelated application, persistence, or user-interface behavior.
- Deserialization must not be treated as validation. Deserialized requests must still pass the appropriate project validation rules before their values are trusted or processed.
- Constructors must not be relied upon as the only validation mechanism for serialized DTOs because `DataContractSerializer` does not construct objects through the normal constructor path during deserialization.
- Changes that affect serialized members must be covered by automated serialization round-trip tests.
- Streams created for serialization tests or serialization operations must follow Section 5.16, Resource Disposal.

Microsoft identifies changes to a data-contract name or namespace, member order, member name, or member data contract as breaking changes. Microsoft also notes that adding a required member can break compatibility with older contract versions (Microsoft, 2021a).

`IsRequired` only indicates that a member must be present in the serialized representation. It does not guarantee that the received value is valid, non-null, non-zero, safe, or acceptable for the application. Therefore, serialization requirements and input validation must remain separate concerns (Microsoft, 2021a).

#### Correct — Explicit Data Contract

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class CreateAccountRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    [DataMember(Order = 3, IsRequired = true)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 4, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } =
        string.Empty;

    [DataMember(Order = 5, EmitDefaultValue = false)]
    public ProfileImageUploadDto? ProfileImage { get; set; }
}
```

The contract explicitly defines which type and members participate in serialization. Required and optional members are distinguishable, the serialized order is explicit, and the shared project namespace identifies the versioned contract family.

#### Incorrect — Implicit and Uncontrolled Contract

```csharp
public sealed class CreateAccountRequest
{
    public string Username { get; set; } = string.Empty;

    public string EmailAddress { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string PreferredLocaleCode { get; set; } =
        string.Empty;

    public ProfileImageUploadDto? ProfileImage { get; set; }
}
```

`DataContractSerializer` can infer contracts for some public types, so this class may still be serializable. However, the serialized representation is no longer explicitly documented through the project's contract conventions.

The project therefore requires explicit `DataContract` and `DataMember` declarations for service-boundary contract types so that changes to the transport representation are intentional and reviewable.

#### **Correct — Serialized Enumeration**

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public enum LoginResultCode
{
    [EnumMember]
    Success,

    [EnumMember]
    InvalidRequest,

    [EnumMember]
    InvalidCredentials,

    [EnumMember]
    AccountInactive,

    [EnumMember]
    ServiceUnavailable
}
```

Because the enumeration itself uses `DataContract`, every value intended to participate in the serialized contract is explicitly marked with `EnumMember`.

Microsoft states that enumeration members in an explicitly declared data contract must use `EnumMemberAttribute` to participate in that contract (Microsoft, 2021b).

#### Incorrect — Breaking Existing Member Order

Original contract:

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;
}
```

Incorrect modification:

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public long AccountId { get; set; }
}
```

Although both properties remain present, their serialized order has been changed. Microsoft classifies changing `DataMemberAttribute.Order` values as a breaking data-contract change (Microsoft, 2021a).

A new member should instead preserve existing order values:

```csharp
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerProfileDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public long AccountId { get; set; }

    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? ProfileImageUri { get; set; }
}
```

#### Correct — Serialization Round-Trip Test

```csharp
private static TContract RoundTrip<TContract>(
    TContract value)
{
    DataContractSerializer serializer =
        new DataContractSerializer(typeof(TContract));

    using MemoryStream stream = new MemoryStream();

    serializer.WriteObject(stream, value);
    stream.Position = 0;

    object? result = serializer.ReadObject(stream);
    object nonNullResult = TestAssert.IsNotNull(result);

    return (TContract)nonNullResult;
}
```

A round-trip test serializes the contract and then deserializes the serialized representation back into the same contract type.

Tests using this helper must verify that the members relevant to the contract retain their expected values after the complete serialization/deserialization process.

#### Incorrect — Assuming Deserialization Validates the Data

```csharp
CreateAccountRequest request =
    serializer.ReadObject(stream)
        as CreateAccountRequest
        ?? throw new SerializationException();

CreateAccount(request);
```

Successful deserialization only establishes that the serialized representation could be converted into the contract type. It does not establish that the username, email address, password, locale, image, or other values satisfy the application's rules.

The deserialized request must still be passed through the project's validation process before the requested operation is performed.

### 5.18 Explicit Local Variable Types

Local variable declarations must use an **explicit type**. The `var` keyword must not be used in project-owned production code or test code.

This is a **project-specific educational convention**, not a C# requirement. Microsoft’s general C# conventions often recommend `var` when the type is obvious, but Microsoft also provides the official `IDE0008` style rule specifically for teams that prefer explicit types. `IDE0008` can be configured as a warning or error, and all three `var` preference options support choosing explicit types (Microsoft, 2022b, 2025f).

#### Correct

```csharp
PlayerController playerController = new PlayerController();
List<Enemy> enemies = new List<Enemy>();
int remainingLives = 3;
```

The variable type is visible directly in every declaration.

#### Incorrect

```csharp
var playerController = new PlayerController();
var enemies = new List<Enemy>();
var remainingLives = 3;
```

The declarations are valid C#, but they violate the explicit-type convention adopted by this project.

### 5.19 Conditional Operator

The conditional operator `?:`, sometimes called the ternary operator, evaluates a Boolean condition and produces the value of one of two expressions.

For an expression in the form `condition ? consequent : alternative`, C# first evaluates `condition`. If the condition is `true`, only `consequent` is evaluated. Otherwise, only `alternative` is evaluated. The two alternatives are never both evaluated. Conditional operators are right-associative, which means that nested conditional expressions are grouped from right to left (Ecma International, 2023).

The availability and evaluation behavior of `?:` are defined by the C# language. Choosing it instead of an `if` statement is a readability convention established by this project.

For this project:

- The conditional operator should be used when one clear Boolean condition selects between exactly two short values or expressions.
- It is appropriate for simple assignments, return values, arguments, and expression-bodied members when it makes the result easier to read than an equivalent `ifelse` statement.
- The condition, consequent, and alternative must each remain understandable without requiring an explanatory comment.
- Nested conditional operators must not be used. An `ifelse` statement, a `switch` statement, a `switch` expression, or a named method should be used when more than two outcomes must be represented.
- The conditional operator must not be used when either alternative requires multiple statements, exception handling, or a separate control-flow operation.
- Method calls may appear in the alternatives when both calls compute the same kind of result and the selected behavior remains immediately clear.
- The conditional operator should not be used primarily to choose between unrelated side effects or workflows.
- The result type must remain clear and must follow the explicit-type rules established in Section 5.18.
- A conditional expression remains a decision and must not be used to disguise excessive complexity. Section 5.9 continues to apply.
- Spaces must appear on both sides of `?` and `:`, consistently with Section 4.6.
- A short conditional expression may remain on one line when it satisfies the limit in Section 4.4.
- When a conditional expression is divided across lines, the `?` and `:` tokens must begin aligned continuation lines, following Section 4.5.

#### Correct

```
internal double WinRate => MatchesPlayed == 0
    ? 0d
    : (double)Wins / MatchesPlayed;
```

The expression uses one clear condition to select between two short numeric values. The multiline layout keeps the condition and both alternatives visually distinct.

Another concise use is acceptable when the complete expression remains easy to read:

```
Cursor cursor = isMoveValid ? Cursors.Hand : Cursors.Arrow;
```

The expression selects one of two closely related values and remains clearer than an equivalent multi-line `if`-`else` assignment.

#### Incorrect

```
MatchOutcome outcome = blackScore > whiteScore
    ? MatchOutcome.BlackWin
    : blackScore == whiteScore
        ? MatchOutcome.Draw
        : MatchOutcome.WhiteWin;
```

The expression nests a second conditional operator inside the alternative branch. Because conditional operators are right-associative, the reader must determine how the conditions are grouped before understanding the three possible outcomes. A named method, `if`-`else` structure, or `switch` expression would communicate the decision more clearly.

---

## 6. Error Handling, Logging, and Diagnostics

### 6.1 Exception Handling

Exceptions must be used for **unexpected or exceptional conditions**, not as a substitute for normal program flow. A `catch` block should only handle an exception when the program can perform a meaningful recovery or restore the application to a known state. Whenever possible, the most specific applicable exception type should be caught instead of the general `Exception` type. Empty `catch` blocks are not permitted.

If an exception cannot be handled correctly at the current level, it should be allowed to propagate. When rethrowing the same exception from a `catch` block, `throw;` must be used instead of `throw exception;` so that the original stack trace is preserved (Microsoft, 2025c).

#### Correct

```csharp
public GameSettings LoadSettings(string filePath)
{
    try
    {
        string json = File.ReadAllText(filePath);
        return DeserializeSettings(json);
    }
    catch (FileNotFoundException)
    {
        return GameSettings.CreateDefault();
    }
}
```

The method catches a specific exception and performs a meaningful recovery by returning valid default settings.

#### Incorrect

```csharp
public GameSettings LoadSettings(string filePath)
{
    try
    {
        string json = File.ReadAllText(filePath);
        return DeserializeSettings(json);
    }
    catch (Exception)
    {
        return null;
    }
}
```

The code catches every possible exception, hides the actual cause of the failure, and may leave the caller with an invalid or unexpected state.

### 6.2 Custom Exceptions

A custom exception must only be created when the predefined .NET exception types do not appropriately represent the error condition. Custom exception classes must derive from `Exception`, use a descriptive name ending in `Exception`, and implement at least the three common constructors: a parameterless constructor, a constructor that receives a message, and a constructor that receives both a message and an inner exception.

Additional properties should only be added when the information is useful for programmatic handling of the exception (Microsoft, 2025c).

#### Correct

```csharp
public class GameConfigurationException : Exception
{
    public GameConfigurationException()
    {
    }

    public GameConfigurationException(string message)
        : base(message)
    {
    }

    public GameConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

The exception has a descriptive domain-specific name, derives from `Exception`, ends with the required `Exception` suffix, and provides the three common constructors recommended by Microsoft.

#### Incorrect

```csharp
public class ConfigError
{
    public string Message { get; set; }
}
```

`ConfigError` does not derive from `Exception`, does not use the `Exception` suffix, and recreates functionality already provided by the .NET exception system.

### 6.3 Logging

The project must use a **centralized logging mechanism** for diagnostic information instead of writing diagnostic output directly to the console, debugger, operating-system event log, or files from application classes.

`Microsoft.Extensions.Logging.ILogger<T>` must be used as the project logging abstraction. The generic category type should identify the class that produces the log entry. `ILogger` may only be used when a generic category cannot reasonably be supplied, such as infrastructure code that creates loggers dynamically.

`ILogger<T>` and the logging-provider model are part of the .NET logging infrastructure; they are not requirements of the C# language. Microsoft recommends obtaining logger instances through dependency injection or an application host instead of constructing separate logging factories throughout the application. Logging providers determine where entries are written and may be changed or combined without changing the classes that produce those entries (Microsoft, 2026g).

For this project:

- Classes that require logging must receive `ILogger<T>` through constructor injection.
- When constructor injection of `ILogger<T>` increases the parameter list of a class with three application dependencies to four, it is recognized as an authorized exception to Section 5.8.
- Logger dependencies that remain unchanged during the object lifetime must be stored in `readonly` fields, as established in Section 5.3.
- Reusable application code must depend on `ILogger<T>` rather than a concrete provider or operating-system logging API.
- Logging factories, providers, filters, and destination-specific settings must be configured only in executable composition roots.
- Provider configuration must remain outside game rules, application use cases, persistence operations, and presentation logic.
- Development builds may use providers intended for interactive diagnostics, such as Debug or Console.
- Released server deployments should use at least one persistent provider for operational warnings, errors, and critical failures.
- Windows Event Log may be selected as a provider when an executable is deployed on Windows, but project-owned classes must not write to it directly.
- Direct calls to `Console.WriteLine`, `Debug.WriteLine`, or operating-system event APIs must not be used as a substitute for centralized logging.
- Logging must not change the result or expected control flow of a game or application operation.

#### Correct

```csharp
public sealed class MatchCoordinator
{
    private readonly ILogger<MatchCoordinator> _logger;

    public MatchCoordinator(ILogger<MatchCoordinator> logger)
    {
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public void StartMatch(Guid matchId)
    {
        _logger.LogInformation(
            "Match {MatchId} started.",
            matchId);

        // Match-starting logic.
    }
}
```

The logger is supplied through the constructor, stored in a `readonly` field, and used through the project logging abstraction. The class does not know which provider or destination receives the entry.

#### Incorrect

```csharp
public sealed class MatchCoordinator
{
    public void StartMatch(Guid matchId)
    {
        EventLog.WriteEntry(
            "Game",
            "Match " + matchId + " started.");

        // Match-starting logic.
    }
}
```

The class depends directly on a Windows-specific destination, constructs an unstructured message, and prevents the logging destination from being changed without modifying application code.

### 6.4 Log Levels

Every log message must use the severity level that corresponds to the importance and effect of the event. The project adopts the standard `Microsoft.Extensions.Logging.LogLevel` categories documented by Microsoft. These categories are provided by the .NET logging infrastructure; their project-specific usage is defined by this standard (Microsoft, 2026g)..

| Level | Project Usage |
| --- | --- |
| `Trace` | Extremely detailed diagnostic information used for specific troubleshooting. |
| `Debug` | Development information useful while debugging the game. |
| `Information` | Important events in the normal execution flow. |
| `Warning` | Unexpected or abnormal situations from which the game can continue. |
| `Error` | A failure that prevents the current operation from completing successfully. |
| `Critical` | A severe failure that threatens continued execution, data integrity, or the overall application. |
| `None` | Disables logging for the configured category. |

For this project:

- `Trace` and `Debug` should normally be disabled in released builds unless they are temporarily required for a controlled investigation.
- `Information` should be used for significant lifecycle, communication, or game events and must not be emitted for every method call.
- An expected validation rejection must not automatically be classified as `Error`.
- `Warning` should be used when an abnormal condition is handled and execution can continue in a known state.
- `Error` must be used when the current operation fails but the application or service can continue processing other operations.
- `Critical` must be reserved for failures that can terminate a process, prevent startup, compromise data integrity, or make continued execution unsafe.
- The same failure must not be logged repeatedly only because it propagates through several layers. It should normally be logged at the boundary that has enough context and responsibility to describe the final failure.
- A log level must describe the technical effect of the event, not how important the associated game action is to the player.

#### Correct

```csharp
_logger.LogInformation(
    "Match {MatchId} completed successfully.",
    matchId);

_logger.LogWarning(
    "Preferred asset {AssetName} was unavailable. The default asset will be used.",
    assetName);

_logger.LogError(
    exception,
    "Unable to persist match {MatchId}.",
    matchId);
```

The normal completion, recoverable fallback, and failed persistence operation use levels that correspond to their technical effects.

#### Incorrect

```csharp
_logger.LogCritical(
    "Player {PlayerId} opened the settings screen.",
    playerId);

_logger.LogInformation(
    "Database operation failed.");
```

Opening a normal screen does not threaten application execution, while a failed database operation must not be classified as normal informational activity.

### 6.5 Log Message Conventions

Log messages must use **static message templates with named placeholders** instead of string concatenation or interpolation. Placeholder names must describe the represented values so that compatible providers can preserve them as structured data.

The .NET analyzer rule `CA2254` detects message templates that vary between logging calls. The analyzer supports the rule established by this project; it does not represent a C# language requirement (Microsoft, 2026c).

When an exception is logged, the exception object must be passed through a logger overload that accepts an `Exception`. Logging only `exception.Message` is insufficient because it discards the exception type, stack trace, inner exception, and other diagnostic information.

Technical log messages must be written in English, as established in Section 2.1. They must not be localized because log consumers, filters, searches, and diagnostic tools require stable technical terminology. Player-facing error messages remain subject to Section 6.7.

A scope groups entries that belong to one logical operation. Microsoft documents `BeginScope` as the mechanism for attaching shared context to the entries produced during that operation (Microsoft, 2026g).

For this project:

- Placeholder names must use PascalCase, such as `{MatchId}`, `{OperationName}`, and `{CorrelationId}`.
- An operation that crosses a process or network boundary must have a correlation identifier available to both sides.
- The correlation identifier must be included in diagnostic entries related to that operation.
- `BeginScope` should be used to attach shared operation context when the configured provider supports scopes.
- Internal identifiers should be preferred over personal information when identifying accounts, players, or matches.
- Passwords, password hashes, salts, session tokens, verification codes, connection strings, encryption keys, and other credentials must never be written to logs.
- Raw e-mail addresses and other personal information must not be logged unless the diagnostic need has been explicitly reviewed and the value is appropriately minimized or redacted.
- Complete request, response, entity, or data-transfer objects must not be logged without reviewing every serialized field.
- Exception messages must not be assumed to be safe merely because the exception was created by project-owned code.
- Log entries should contain enough context to identify the failed operation without reproducing all of its input data.
- Recurring operational events should use stable message templates so that entries can be filtered and grouped reliably.

#### Correct

```csharp
public void ReportMatchFailure(
    Exception exception,
    Guid matchId,
    string correlationId)
{
    using IDisposable? scope = _logger.BeginScope(
        "CorrelationId:{CorrelationId}",
        correlationId);

    _logger.LogError(
        exception,
        "Failed to persist match {MatchId}.",
        matchId);
}
```

The method preserves the exception, uses a static structured template, identifies the affected match, and attaches a correlation identifier without exposing credentials or personal information.

#### Incorrect

```csharp
_logger.LogError(
    $"Login for {emailAddress} failed with password {password}, " +
    $"token {sessionToken}, and error {exception.Message}.");
```

The message uses interpolation, exposes credentials and personal information, and records only the exception message instead of passing the exception object to the logger.

### 6.6 Assertions

`Debug.Assert` must be used to verify **internal conditions that should always be true when the program is implemented correctly**. Assertions are intended to reveal programming errors and invalid internal states during development.

Assertions must not be used as the primary mechanism for validating player input, handling file errors, network failures, or other expected runtime conditions. `Debug` methods are normally excluded from Release builds, so application behavior must never depend on an assertion executing (Microsoft, 2025a).

#### Correct

```csharp
Debug.Assert(
    player.MaxHealth > 0,
    "Player maximum health must always be greater than zero.");
```

A player object with a non-positive maximum health represents an internal state that should be impossible if the program is correctly configured.

#### Incorrect

```csharp
Debug.Assert(
    File.Exists(savePath),
    "The save file must exist.");
```

A save file can legitimately be missing during normal execution. This condition requires normal runtime handling rather than a development assertion.

### 6.7 Player-Facing Error Messages

Messages displayed to players must be **clear, specific, user-centered, and actionable**. They should explain what happened and, when useful, why it happened and what the player can do next. Technical implementation details such as exception class names, stack traces, internal method names, or source-code information must not be shown as the primary player-facing message.

When the application can identify the specific cause of an error, it should display a message for that cause rather than a generic message containing several possible explanations. Messages should use terminology understandable to the player and should avoid unnecessary technical jargon (Microsoft, 2025h).

#### Correct

```csharp
Your saved game could not be loaded because the file is damaged.
Start a new game or restore a previous backup.
```

The message states the problem, identifies the known cause, and gives the player a useful next action.

#### Incorrect

```csharp
System.IO.InvalidDataException in SaveGameManager.Load():
Deserialization failed at offset 0x000014A2.
```

The message exposes internal implementation details and does not explain the problem from the player’s perspective or provide a useful action.

### 6.8 **Unhandled Exceptions and Diagnostic Reporting**

Each executable process must configure a centralized last-chance mechanism for reporting exceptions that reach an application or service boundary without being handled. Its purpose is to preserve diagnostic information before the process terminates or before a controlled failure response is produced.

A last-chance exception handler is a diagnostic safety net, not a substitute for the exception-handling rules in Section 6.1. Recording an exception does not make the application state valid. Execution must not continue after an unhandled exception unless the boundary can establish that recovery is safe and the application remains in a known state.

At a remote service boundary, expected failures should be represented through documented result or fault contracts. Unexpected exceptions should be logged on the server side and converted into a controlled failure that does not expose implementation details. Microsoft documents centralized service error handlers as an appropriate location for logging unexpected service failures and converting exceptions into faults (Microsoft, 2021c).

A diagnostic entry for an unhandled exception should include, when available:

- The complete exception supplied to the logger.
- The application version.
- The executable role or process name.
- The operation being performed.
- The correlation identifier.
- Relevant non-sensitive resource identifiers.
- Platform or runtime information needed to reproduce the failure.

For this project:

- Client and server processes must configure their diagnostic boundaries independently.
- Server-side failures must be logged by the server; internal server exceptions must not be transferred to the client for logging.
- A client should log presentation, local-resource, proxy, timeout, and communication failures that it can directly observe.
- A server should log failures in remote operations, application processing, persistence, and server-owned resources.
- An unexpected remote failure must be logged once at the boundary responsible for converting it into a controlled response.
- Stack traces, exception type names, database messages, file-system paths, and internal method names must not be included in player-facing responses.
- A controlled response may include a correlation identifier so that a reported problem can be matched with server-side diagnostics.
- Released builds must use a provider capable of preserving `Error` and `Critical` entries after the process exits.
- Diagnostic handling must follow the sensitive-data rules in Section 6.5.
- Empty catch blocks and broad catch blocks that silently discard unexpected exceptions are not permitted.

#### Correct

```csharp
public FailureResponse HandleUnexpectedFailure(
    Exception exception,
    string correlationId)
{
    _logger.LogCritical(
        exception,
        "Unhandled service failure. CorrelationId: {CorrelationId}.",
        correlationId);

    return new FailureResponse(
        "UnexpectedFailure",
        correlationId);
}
```

The exception is preserved in server-side diagnostics, while the response contains only a stable error code and the correlation identifier needed to locate the corresponding entry.

#### Incorrect

```csharp
public FailureResponse HandleUnexpectedFailure(
    Exception exception)
{
    return new FailureResponse(
        exception.GetType().Name,
        exception.ToString());
}
```

The exception is not recorded through the centralized logger, and its type, message, stack trace, and internal implementation details are exposed across the application boundary.

---

## 7. Comments and Documentation

### 7.1 General Comment Rules

Comments must be used to improve the understanding of code by explaining its purpose, intent, constraints, or other information that is not immediately clear from the implementation itself. Brief explanations must use single-line comments (`//`). Comments written as complete sentences must begin with an uppercase letter and end with a period. When a comment is wrapped across multiple lines, only the final line must contain the final punctuation. Standard structural markers defined by this standard, such as `// Arrange`, `// Act`, and `// Assert`, are exempt from the final-period requirement. Comments should not simply repeat what an obvious statement already expresses (Microsoft, 2025f).

#### Correct

```csharp
// Prevent movement while the player is interacting with the pause menu.
if (isGamePaused)
{
    return;
}
```

The comment explains the reason for the condition and provides useful context about the intended game behavior.

#### Incorrect

```csharp
if (isGamePaused)
{
    return; //return if paused
}
```

The comment is placed at the end of an executable statement, does not begin with an uppercase letter or end with a period, and only repeats information that is already clear from the code.

### 7.2 Implementation Comments

Implementation comments must be used when a section of code contains a non-obvious decision, workaround, limitation, algorithmic choice, or behavior that another developer might not understand by reading the code alone. They should explain **why** the implementation exists or what constraint it addresses rather than narrating individual statements. Brief implementation explanations must use `//`; long block comments using `/* ... */` should generally be avoided in favor of clear code and concise comments (Microsoft, 2025f).

#### Correct

```csharp
// Reset vertical velocity before applying the jump impulse so repeated
// jumps produce the same initial force.
playerVelocity.Y = 0f;
playerVelocity.Y += jumpForce;
```

The comment explains the reason behind a potentially non-obvious implementation decision.

#### Incorrect

```csharp
// Set Y velocity to zero.
playerVelocity.Y = 0f;

// Add jump force.
playerVelocity.Y += jumpForce;
```

The comments only translate the code into natural language and do not provide information that helps another developer understand the implementation.

### 7.3 XML Documentation Comments

XML documentation comments must use the `///` syntax and must immediately precede the type or member they document. Documentation must contain well-formed XML. At minimum, documented types and members must include a `<summary>` element written as a complete sentence. When applicable, documentation should also use elements such as `<param>`, `<returns>`, `<exception>`, and `<remarks>` to describe information that is part of the member’s contract (Microsoft, 2025g, 2026k).

#### Correct

```csharp
/// <summary>
/// Calculates the final damage dealt by an attack.
/// </summary>
/// <param name="baseDamage">The base damage of the attack.</param>
/// <param name="multiplier">The multiplier applied to the base damage.</param>
/// <returns>The final calculated damage.</returns>
public float CalculateDamage(float baseDamage, float multiplier)
{
    return baseDamage * multiplier;
}
```

The documentation uses the `///` syntax, immediately precedes the documented member, contains well-formed XML, and describes the method, its parameters, and its return value.

#### Incorrect

```csharp
/// Calculates damage
/// <param name="damage">Damage value</param>
public float CalculateDamage(float baseDamage, float multiplier)
{
    return baseDamage * multiplier;
}
```

The documentation does not provide a `<summary>` element, does not describe all parameters, and references a parameter named `damage` that does not exist in the method signature.

### 7.4 Public API Documentation

All publicly visible production API types and members must have XML documentation comments. Test classes and test methods whose public visibility is required by the selected testing framework are exempt unless they are part of a reusable public API. At minimum, each public type or member must contain a `<summary>` that clearly describes its purpose. Public methods must also document their parameters, return values, and relevant exceptions when applicable. Public API documentation must be complete, correct, contextual, and written clearly enough for another developer to understand how the API should be used without reading its internal implementation (Microsoft, 2025i, 2026k).

#### Correct

```csharp
/// <summary>
/// Represents the health state of a player character.
/// </summary>
public class PlayerHealth
{
    /// <summary>
    /// Reduces the player's current health by the specified amount.
    /// </summary>
    /// <param name="damage">
    /// The amount of health to remove.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="damage"/> is negative.
    /// </exception>
    public void TakeDamage(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        // Implementation.
    }
}
```

Both the publicly visible class and its public method are documented. The method documentation describes its purpose, parameter, and relevant exception.

#### Incorrect

```csharp
public class PlayerHealth
{
    public void TakeDamage(int damage)
    {
        // Implementation.
    }
}
```

The publicly visible class and method have no XML documentation, so developers using the API receive no documented description of their purpose or contract.

### 7.5 `TODO` Comments

`TODO` comments must identify a **specific piece of work that is intentionally left to be completed later**. They must use the format `// TODO: Description.` and clearly state the remaining action. Vague descriptions such as `fix later`, `do this`, or `finish` are not permitted because they do not provide enough information for another team member to understand the pending task.

Visual Studio recognizes `TODO` as a predefined Task List token and can automatically collect these comments so developers can locate pending work. Although Visual Studio treats the token as case-insensitive, this project requires uppercase `TODO` for consistency with the rest of the standard (Microsoft, 2025l).

#### Correct

```csharp
// TODO: Replace the temporary enemy spawn positions with level configuration data.
SpawnEnemies(defaultSpawnPositions);
```

The comment identifies exactly what remains to be implemented and provides enough information for another team member to understand the task.

#### Incorrect

```csharp
// todo: fix this later
SpawnEnemies(defaultSpawnPositions);
```

The description is vague and does not explain what must be changed. It also violates the project’s standardized uppercase `TODO` format and sentence-style comment convention.

### 7.6 `FIXME` Comments

`FIXME` comments must identify code that is **known to contain an incorrect, defective, or temporary behavior that requires correction**. They must use the format `// FIXME: Description.` and describe the known problem clearly enough for another developer to understand what is wrong.

`FIXME` is not a predefined Visual Studio Task List token. Therefore, its meaning and uppercase format are a **project-specific convention**, not a C# language rule. Visual Studio officially supports custom Task List tokens, so the team may configure `FIXME` as a custom token to make these comments automatically discoverable together with `TODO` comments (Microsoft, 2025l).

#### Correct

```csharp
// FIXME: Enemy damage is applied twice when two collision callbacks occur in the same frame.
ApplyEnemyDamage(enemyDamage);
```

The comment identifies a known defect and describes the exact behavior that requires correction.

#### Incorrect

```csharp
// FIXME: broken
ApplyEnemyDamage(enemyDamage);
```

The comment indicates that a problem exists but does not explain what is incorrect, when it occurs, or what another developer should investigate.

---

## 8. Input Validation and Security

### 8.1 Trust Boundaries

Data received from outside a component's trusted control must be validated before it is used to make decisions or change protected state.

A trust boundary separates components or sources with different control over data and permissions. The client-to-server connection is a trust boundary because the server does not control the caller's program. Client-side checks can improve feedback, but callers can bypass them. OWASP recommends server-side validation even when the client validates the same input, and distinguishes input validation from authorization (OWASP Foundation, n.d.).

WCF transfer security protects communication through authentication, confidentiality, and integrity. Authorization determines access to resources and remains a separate requirement (Microsoft, 2023j). An encrypted connection does not establish that a proposed username, account identifier, or game action is acceptable to the application.

For this project:

- The WPF client may perform preventive checks, such as detecting an invalid username or calculating available moves for display. The server must independently enforce the applicable rules before accepting an operation or changing authoritative state.
- Operations that require an authenticated player must resolve the caller from a server-validated session token. A client-supplied account identifier, display name, or validation flag must not establish the caller's identity or permissions. Operations explicitly available to guests or unauthenticated callers must enforce their own access rules.
- The server must check the caller's permission for the requested operation and target. A correctly formatted identifier or an existing account is not sufficient authorization. For changes to the caller's own profile, the affected account must come from the validated session.
- WCF adapters must pass received values to the appropriate server use case. Application and domain rules must be enforced before the protected action, even when the official client normally checks those values. A service method must not bypass these rules by writing directly to persistence.
- Successful deserialization must not establish trust in received values. Service data must follow Section 5.17, Serialization, and the applicable validation rules before use.
- Stored data and file metadata must not be assumed valid solely because they come from server storage. When their origin or integrity is outside the receiving component's control, validate the assumptions needed for the intended use. This does not require repeating every validation between internal methods that operate on already validated data.
- Rejected operations must leave the protected action unperformed and use the declared service fault where applicable. Error handling and diagnostics must follow Section 6 without exposing credentials, session tokens, or internal storage details.

These responsibilities do not require a shared validation engine or an additional wrapper for each operation. Existing domain policies and use cases should enforce the rules where the decision belongs.

#### Correct

```csharp
public void ChangeUsername(string sessionToken, string username)
{
    long accountId = RequireActiveCaller(sessionToken);
    string normalized = username?.Trim() ?? string.Empty;
    if (UsernamePolicy.Validate(normalized) != UsernameValidationError.None)
    {
        throw new ProfileRejectedException(ProfileRejection.InvalidUsername);
    }

    _profiles.ChangeUsername(accountId, normalized);
}
```

This example follows `PlayerProfileEditor.ChangeUsername`. `RequireActiveCaller` resolves the session on the server and checks that the account is active. The method then applies the server's username policy before asking persistence to change that account's name. The store checks availability; client validation is not a precondition for the server checks.

#### Incorrect

```csharp
public void ChangeUsername(string sessionToken, string username)
{
    long accountId = RequireActiveCaller(sessionToken);
    string normalized = username?.Trim() ?? string.Empty;

    // The client already checked the username.
    _profiles.ChangeUsername(accountId, normalized);
}
```

The caller is still authenticated, but the server omits its username validation because it assumes that the official client performed it. Another caller can invoke the same operation with a value that the client would reject. Authentication does not replace input validation.

### 8.2 Input Validation

### 8.3 File System Access

### 8.4 Save Data Security

### 8.5 Network Input Validation

### 8.6 Deserialization Security

### 8.7 Credential and Secret Management

### 8.8 Sensitive Configuration

---

## 9. Testing and Quality Assurance

### 9.1 Testing Strategy

The project must use **automated testing at different levels according to what is being verified**. Unit tests must be used to verify isolated game logic, while integration tests must be used when the correct behavior depends on two or more components working together. Whenever behavior can be sufficiently verified with a unit test, the unit test should be preferred because it is faster, simpler, and easier to isolate. Integration tests should be reserved for interactions that cannot be adequately verified in isolation (Microsoft, 2026m; Reese, 2025).

For this project, new or modified testable logic should include the appropriate automated tests before the feature is considered complete. The objective is not to create the largest possible number of tests, but to test important behavior at the lowest practical testing level.

#### Correct

```csharp
Feature: Player combat

Unit tests:
- DamageCalculatorTests
- CriticalHitCalculatorTests

Integration tests:
- CombatSystemIntegrationTests
  - Verifies that Weapon, DamageCalculator, and PlayerHealth
    work together correctly.
```

The testing strategy uses unit tests for isolated calculations and an integration test only where multiple game components must interact.

#### Incorrect

```csharp
Feature: Player combat

Testing:
- Launch the game.
- Attack an enemy manually.
- If the health bar changes, consider the feature tested.
```

This approach relies only on manual verification. It does not provide repeatable automated tests and makes regression detection more difficult after future changes.

### 9.2 Unit Testing

A unit test must verify a **single unit of behavior in isolation**, such as a method, class, or small piece of game logic. Unit tests must not depend directly on external infrastructure such as the file system, databases, or network resources. External dependencies should be replaced with controlled substitutes when necessary so that the result depends only on the code being tested.

Unit tests should be **fast, isolated, repeatable, self-checking, and easy to understand**. Each test should focus on one behavior so that a failure clearly indicates what functionality is incorrect (Microsoft, 2026m; Reese, 2025).

#### Correct

```csharp
[TestClass]
public class DamageCalculatorTests
{
    [TestMethod]
    public void CalculateDamage_ValidAttack_ReturnsExpectedDamage()
    {
        // Arrange
        DamageCalculator damageCalculator = new DamageCalculator();

        // Act
        int actualDamage = damageCalculator.CalculateDamage(20, 2);

        // Assert
        Assert.AreEqual(40, actualDamage);
    }
}
```

The test verifies one isolated behavior, requires no external resources, produces the same result each time, and automatically determines whether the result is correct.

#### Incorrect

```csharp
[TestClass]
public class DamageCalculatorTests
{
    [TestMethod]
    public void CalculateDamage_TestEverything()
    {
        DamageCalculator damageCalculator = new DamageCalculator();

        File.WriteAllText("damage.txt", "20");

        int damage = damageCalculator.CalculateDamage(
            int.Parse(File.ReadAllText("damage.txt")),
            2);

        Console.WriteLine(damage);
    }
}
```

This test depends unnecessarily on the file system, does not isolate the damage calculation, and does not contain an assertion that automatically determines whether the behavior is correct.

### 9.3 Integration Testing

An integration test must verify that **two or more real software components work correctly together**. Unlike unit tests, integration tests cover a broader portion of the system and may include infrastructure such as the file system, persistent storage, networking, or other services when those elements form part of the interaction being tested.

Integration tests require more setup and normally execute more slowly than unit tests. For this reason, they should focus on important interactions between components rather than duplicating every scenario already covered by unit tests. When a behavior can be adequately verified in isolation, a unit test should be preferred (Microsoft, 2026m; van der Til et al., 2026).

#### Correct

```csharp
[TestClass]
public class SaveGameIntegrationTests
{
    [TestMethod]
    public void SaveAndLoad_ValidGameState_RestoresPlayerHealth()
    {
        // Arrange
        string savePath = Path.GetTempFileName();

        try
        {
            JsonSaveRepository repository = new JsonSaveRepository(savePath);
            SaveGameService saveService = new SaveGameService(repository);
            GameState gameState = new GameState
            {
                PlayerHealth = 75
            };

            // Act
            saveService.Save(gameState);
            GameState loadedState = saveService.Load();

            // Assert
            Assert.AreEqual(75, loadedState.PlayerHealth);
        }
        finally
        {
            File.Delete(savePath);
        }
    }
}
```

The test verifies the interaction between the save service, repository, serialization process, and file system rather than testing only one class in isolation.

#### Incorrect

```csharp
[TestClass]
public class SaveGameIntegrationTests
{
    [TestMethod]
    public void SaveAndLoad_ValidGameState_RestoresPlayerHealth()
    {
        Mock<ISaveRepository> repository = new Mock<ISaveRepository>();

        repository
            .Setup(saveRepository => saveRepository.Load())
            .Returns(new GameState
            {
                PlayerHealth = 75
            });

        SaveGameService saveService = new SaveGameService(repository.Object);

        GameState loadedState = saveService.Load();

        Assert.AreEqual(75, loadedState.PlayerHealth);
    }
}
```

Although the test is named as an integration test, the repository is replaced with a mock. Therefore, the interaction between the actual persistence components is not being tested. This behavior is closer to a unit test than an integration test.

### 9.4 Arrange-Act-Assert Structure

Every unit test must follow the **Arrange-Act-Assert (AAA)** pattern to make its purpose easy to understand.

- **Arrange:** Create and configure the objects, input values, and initial state required by the test.
- **Act:** Execute the single behavior being tested.
- **Assert:** Verify that the actual result matches the expected result.

For this school project, the three phases must be visually separated by blank lines and identified using the comments `// Arrange`, `// Act`, and `// Assert`. Each test should normally contain only **one Act operation** so that a failure can be associated with one specific behavior (Microsoft, 2026o; Reese, 2025).

#### Correct

```csharp
[TestClass]
public class PlayerHealthTests
{
    [TestMethod]
    public void TakeDamage_ValidDamage_DecreasesHealth()
    {
        // Arrange
        PlayerHealth playerHealth = new PlayerHealth(100);

        // Act
        playerHealth.TakeDamage(30);

        // Assert
        Assert.AreEqual(70, playerHealth.CurrentHealth);
    }
}
```

The test clearly separates its preparation, the behavior being executed, and the verification of the expected result.

#### Incorrect

```csharp
[TestClass]
public class PlayerHealthTests
{
    [TestMethod]
    public void TakeDamage_ValidDamage_DecreasesHealth()
    {
        PlayerHealth playerHealth = new PlayerHealth(100);
        playerHealth.TakeDamage(30);
        playerHealth.TakeDamage(10);
        Assert.AreEqual(60, playerHealth.CurrentHealth);
    }
}
```

The phases are not clearly separated and the test performs more than one Act operation. If the test fails, it becomes less clear which operation caused the incorrect behavior.

---

## 10. Version Control and Collaboration

---

## 11. Automated Enforcement and Compliance

---

## 12. References

Ecma International. (2023, December). *ECMA-334: C# language specification* (7th ed.). [https://ecma-international.org/publications-and-standards/standards/ecma-334/](https://ecma-international.org/publications-and-standards/standards/ecma-334/)

Esposito, D. (2014, October). Cutting edge: Source code readability tips. *MSDN Magazine, 29*(10). Microsoft. [https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/october/cutting-edge-source-code-readability-tips](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/october/cutting-edge-source-code-readability-tips)

GitLab. (n.d.). *JavaScript style guide*. GitLab Docs. Retrieved August 29, 2026, from [https://docs.gitlab.com/development/fe_guide/style/javascript/](https://docs.gitlab.com/development/fe_guide/style/javascript/)

Google. (n.d.). *C# at Google Style Guide*. Google Style Guides. Retrieved August 29, 2026, from [https://google.github.io/styleguide/csharp-style.html](https://google.github.io/styleguide/csharp-style.html)

JetBrains. (2026, March 31). *Invert ‘if’ statement to reduce nesting*. Inspectopedia Documentation. [https://www.jetbrains.com/help/inspectopedia/InvertIf.html](https://www.jetbrains.com/help/inspectopedia/InvertIf.html)

Microsoft. (2020a, October 14). *No-tracking queries*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking](https://learn.microsoft.com/en-us/ef/ef6/querying/no-tracking)

Microsoft. (2020b, October 14). *Querying and finding entities*. Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/querying/](https://learn.microsoft.com/en-us/ef/ef6/querying/)

Microsoft. (2020c, October 14). *Working with DbContext.* Microsoft Learn. [https://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext](https://learn.microsoft.com/en-us/ef/ef6/fundamentals/working-with-dbcontext)

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

Microsoft. (2025e, October 30). *Code metrics—Cyclomatic complexity*. Microsoft Learn. [https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity](https://learn.microsoft.com/en-us/visualstudio/code-quality/code-metrics-cyclomatic-complexity)

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

Microsoft. (2026j, January 26). `*readonly` (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly)

Microsoft. (2026k, June 22). *Recommended XML tags for C# documentation comments*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)

Microsoft. (2026l, June 5). *Records (C# reference)*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)

Microsoft. (2026m, April 27). *Testing in .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/](https://learn.microsoft.com/en-us/dotnet/core/testing/)

Microsoft. (2026n, May 26). *Tutorial: Express your design intent with nullable and non-nullable reference types*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/nullable-reference-types)

Microsoft. (2026o, June 24). *Write tests with MSTest*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests)

Microsoft. (2026p, March 23). *Handle and raise events*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/standard/events/](https://learn.microsoft.com/en-us/dotnet/standard/events/)

Microsoft. (2026q, January 20). *using statement - ensure the correct use of disposable objects*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using)

OWASP Foundation. (n.d.). *Input validation cheat sheet*. OWASP Cheat Sheet Series. Retrieved October 22, 2026, from [https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html)

Reese, J. (2025, March 22). *Unit testing best practices for .NET*. Microsoft Learn. [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

Unity Technologies. (n.d.-a). *Cloud Diagnostics*. Unity Documentation. Retrieved August 29, 2026, from [https://docs.unity.com/en-us/cloud-diagnostics](https://docs.unity.com/en-us/cloud-diagnostics)

Unity Technologies. (n.d.-b). *Naming and code style tips for C# scripting in Unity*. Unity. Retrieved August 29, 2026, from [https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)

Unity Technologies. (n.d.-c). *Set up custom reports with Diagnostics*. Unity Documentation. Retrieved August 29, 2026, from [https://docs.unity.com/en-us/cloud/developer-data/custom-reports](https://docs.unity.com/en-us/cloud/developer-data/custom-reports)

Unity Technologies. (n.d.-d). *View Diagnostics for your project*. Unity Documentation. Retrieved August 29, 2026, from [https://docs.unity.com/en-us/cloud/developer-data/diagnostics](https://docs.unity.com/en-us/cloud/developer-data/diagnostics)

van der Til, J., Costello, M., & Calvarro Nelson, J. (2026, March 10). *Integration tests in [ASP.NET](http://asp.net/) Core*. Microsoft Learn. [https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

---

## 13. AI Use Declaration

Artificial Intelligence tools were used as a **support resource** during the creation of this development standard.

AI was used to help with:

- Organizing and structuring the document.
- Improving the wording of some rules.
- Generating example code for review.
- Comparing different coding recommendations.
- Identifying possible inconsistencies between sections.
- Assisting in the search and organization of references.

The final content was reviewed by the team before being included in the standard. AI-generated information was not accepted automatically; rules and recommendations were verified with official documentation, technical standards, and other reliable sources whenever applicable.

The team remains responsible for the accuracy, consistency, and final decisions contained in this document.

AI tools were used as an assistant, not as a replacement for the team’s analysis, understanding, or responsibility.
