using System;
using System.Globalization;

namespace ReversiGameSystem.Server.Domain.Accounts;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static PasswordValidationStatus Validate(string password)
    {
        int characterCount = 0;
        PasswordCharacterClasses characterClasses = PasswordCharacterClasses.None;

        for (int index = 0; index < password.Length; index++)
        {
            if (!IsSupportedCodePoint(password, index))
            {
                return PasswordValidationStatus.UnsupportedCharacter;
            }

            characterClasses |= GetCharacterClass(password, index);

            if (char.IsHighSurrogate(password[index]))
            {
                index++;
            }

            characterCount++;
            if (characterCount > MaximumLength)
            {
                return PasswordValidationStatus.TooLong;
            }
        }

        if (characterCount < MinimumLength)
        {
            return PasswordValidationStatus.TooShort;
        }

        return ValidateCharacterClasses(characterClasses);
    }

    private static PasswordValidationStatus ValidateCharacterClasses(
        PasswordCharacterClasses characterClasses)
    {
        if ((characterClasses & PasswordCharacterClasses.UppercaseLetter) == 0)
        {
            return PasswordValidationStatus.MissingUppercaseLetter;
        }

        if ((characterClasses & PasswordCharacterClasses.LowercaseLetter) == 0)
        {
            return PasswordValidationStatus.MissingLowercaseLetter;
        }

        if ((characterClasses & PasswordCharacterClasses.Digit) == 0)
        {
            return PasswordValidationStatus.MissingDigit;
        }

        return (characterClasses & PasswordCharacterClasses.Symbol) == 0
            ? PasswordValidationStatus.MissingSymbol
            : PasswordValidationStatus.Success;
    }

    private static PasswordCharacterClasses GetCharacterClass(string password, int index)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(password, index);
        if (category == UnicodeCategory.UppercaseLetter)
        {
            return PasswordCharacterClasses.UppercaseLetter;
        }

        if (category == UnicodeCategory.LowercaseLetter)
        {
            return PasswordCharacterClasses.LowercaseLetter;
        }

        if (category == UnicodeCategory.DecimalDigitNumber)
        {
            return PasswordCharacterClasses.Digit;
        }

        return char.IsPunctuation(password, index) || char.IsSymbol(password, index)
            ? PasswordCharacterClasses.Symbol
            : PasswordCharacterClasses.None;
    }

    private static bool IsSupportedCodePoint(string password, int index)
    {
        char character = password[index];
        if (char.IsLowSurrogate(character))
        {
            return false;
        }

        if (char.IsHighSurrogate(character)
            && (index == password.Length - 1 || !char.IsLowSurrogate(password[index + 1])))
        {
            return false;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(password, index);
        return category != UnicodeCategory.Control
            && category != UnicodeCategory.Format
            && category != UnicodeCategory.LineSeparator
            && category != UnicodeCategory.ParagraphSeparator;
    }

    [Flags]
    private enum PasswordCharacterClasses
    {
        None = 0,
        UppercaseLetter = 1,
        LowercaseLetter = 2,
        Digit = 4,
        Symbol = 8
    }
}

public enum PasswordValidationStatus
{
    Success,
    TooShort,
    TooLong,
    UnsupportedCharacter,
    MissingUppercaseLetter,
    MissingLowercaseLetter,
    MissingDigit,
    MissingSymbol
}
