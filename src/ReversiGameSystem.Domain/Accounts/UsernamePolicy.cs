namespace ReversiGameSystem.Domain.Accounts;

/// <summary>
/// Defines the username rules shared by account clients and application services.
/// </summary>
public static class UsernamePolicy
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 32;

    public static UsernameValidationError Validate(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return UsernameValidationError.Missing;
        }

        if (username.Length < MinimumLength || username.Length > MaximumLength)
        {
            return UsernameValidationError.InvalidLength;
        }

        if (!char.IsLetterOrDigit(username[0]))
        {
            return UsernameValidationError.InvalidCharacters;
        }

        foreach (char character in username)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return UsernameValidationError.InvalidCharacters;
            }
        }

        return UsernameValidationError.None;
    }
}

public enum UsernameValidationError
{
    None,
    Missing,
    InvalidLength,
    InvalidCharacters
}
