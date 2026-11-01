namespace ReversiGameSystem.Server.Domain.Accounts;

/// <summary>
/// Defines the username rules shared by account clients and application services.
/// </summary>
public static class UsernamePolicy
{
    /// <summary>
    /// Represents the minimum length.
    /// </summary>
    public const int MinimumLength = 3;
    /// <summary>
    /// Represents the maximum length.
    /// </summary>
    public const int MaximumLength = 32;

    /// <summary>
    /// Performs the validate operation.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>The result of the operation.</returns>
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

/// <summary>
/// Defines the supported username validation error values.
/// </summary>
public enum UsernameValidationError
{
    /// <summary>
    /// Represents none.
    /// </summary>
    None,
    /// <summary>
    /// Represents missing.
    /// </summary>
    Missing,
    /// <summary>
    /// Represents invalid length.
    /// </summary>
    InvalidLength,
    /// <summary>
    /// Represents invalid characters.
    /// </summary>
    InvalidCharacters
}
