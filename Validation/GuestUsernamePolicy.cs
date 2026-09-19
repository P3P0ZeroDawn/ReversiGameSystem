namespace ReversiGameSystem.Validation;

/// <summary>
/// Normalizes the name supplied for a temporary guest session.
/// </summary>
internal static class GuestUsernamePolicy
{
    internal static UsernameValidationError Validate(string name)
    {
        return UsernamePolicy.Validate(Normalize(name));
    }

    internal static string Normalize(string name)
    {
        return name?.Trim() ?? string.Empty;
    }

}
