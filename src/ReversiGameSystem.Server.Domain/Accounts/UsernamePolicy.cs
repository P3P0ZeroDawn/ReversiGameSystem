namespace ReversiGameSystem.Server.Domain.Accounts;

public static class UsernamePolicy
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 32;

    public static UsernameValidationStatus Validate(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return UsernameValidationStatus.Missing;
        }

        if (username.Length < MinimumLength || username.Length > MaximumLength)
        {
            return UsernameValidationStatus.InvalidLength;
        }

        if (!char.IsLetterOrDigit(username[0]))
        {
            return UsernameValidationStatus.InvalidCharacters;
        }

        foreach (char character in username)
        {
            if (!char.IsLetterOrDigit(character) && character != '_')
            {
                return UsernameValidationStatus.InvalidCharacters;
            }
        }

        return UsernameValidationStatus.Success;
    }
}

public enum UsernameValidationStatus
{
    Success,
    Missing,
    InvalidLength,
    InvalidCharacters
}
