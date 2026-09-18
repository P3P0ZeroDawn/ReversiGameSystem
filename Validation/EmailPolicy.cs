using System;
using System.Globalization;

namespace ReversiGameSystem.Validation;

/// <summary>
/// Checks the email address syntax accepted by the registration form.
/// </summary>
internal static class EmailPolicy
{
    private const int MaximumAddressLength = 254;
    private const int MaximumLocalPartLength = 64;
    private const int MaximumDomainLength = 253;
    private const int MaximumDomainLabelLength = 63;
    private const int MinimumDomainLabelCount = 2;
    private const int MinimumTopLevelDomainLength = 2;
    private const string AllowedLocalSymbols = "!#$%&'*+-/=?^_`{|}~";

    internal static EmailValidationError Validate(string email)
    {
        if (email.Length > MaximumAddressLength)
        {
            return EmailValidationError.TooLong;
        }

        int atSignIndex = email.IndexOf('@');
        if (atSignIndex < 1 || atSignIndex != email.LastIndexOf('@'))
        {
            return EmailValidationError.InvalidStructure;
        }

        string localPart = email.Substring(0, atSignIndex);
        if (!IsValidLocalPart(localPart))
        {
            return EmailValidationError.InvalidLocalPart;
        }

        string domain = email.Substring(atSignIndex + 1);
        if (!TryGetValidAsciiDomain(domain, out string asciiDomain))
        {
            return EmailValidationError.InvalidDomain;
        }

        return localPart.Length + 1 + asciiDomain.Length <= MaximumAddressLength
            ? EmailValidationError.None
            : EmailValidationError.TooLong;
    }

    private static bool IsValidLocalPart(string localPart)
    {
        if (localPart.Length == 0 || localPart.Length > MaximumLocalPartLength)
        {
            return false;
        }

        for (int index = 0; index < localPart.Length; index++)
        {
            char character = localPart[index];
            if (character == '.')
            {
                if (index == 0 || index == localPart.Length - 1 || localPart[index - 1] == '.')
                {
                    return false;
                }
            }
            else if (!IsAsciiLetterOrDigit(character) && AllowedLocalSymbols.IndexOf(character) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetValidAsciiDomain(string domain, out string asciiDomain)
    {
        asciiDomain = string.Empty;
        if (domain.Length == 0 || domain.IndexOf('@') >= 0)
        {
            return false;
        }

        try
        {
            asciiDomain = new IdnMapping().GetAscii(domain);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (asciiDomain.Length > MaximumDomainLength)
        {
            return false;
        }

        string[] labels = asciiDomain.Split('.');
        if (labels.Length < MinimumDomainLabelCount
            || labels[labels.Length - 1].Length < MinimumTopLevelDomainLength)
        {
            return false;
        }

        foreach (string label in labels)
        {
            if (!IsValidDomainLabel(label))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidDomainLabel(string label)
    {
        if (label.Length == 0 || label.Length > MaximumDomainLabelLength
            || label[0] == '-' || label[label.Length - 1] == '-')
        {
            return false;
        }

        foreach (char character in label)
        {
            if (!IsAsciiLetterOrDigit(character) && character != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return (character >= 'a' && character <= 'z')
            || (character >= 'A' && character <= 'Z')
            || (character >= '0' && character <= '9');
    }
}

internal enum EmailValidationError
{
    None,
    TooLong,
    InvalidStructure,
    InvalidLocalPart,
    InvalidDomain
}
