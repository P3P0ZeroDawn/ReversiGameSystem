namespace ReversiGameSystem.Validation;

/// <summary>
/// Validates account usernames before they are submitted to the server.
/// </summary>
internal static class UsernamePolicy
{
    internal const int MinimumLength = 3;
    internal const int MaximumLength = 32;

    internal static UsernameValidationError Validate(string username)
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

internal enum UsernameValidationError
{
    None,
    Missing,
    InvalidLength,
    InvalidCharacters
}
