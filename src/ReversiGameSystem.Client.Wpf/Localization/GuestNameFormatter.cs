namespace ReversiGameSystem.Localization;

/// <summary>
/// Adds the localized guest marker only when a name is presented to the user.
/// </summary>
internal static class GuestNameFormatter
{
    internal static string Format(string guestName)
    {
        return string.Format(
            LocalizationManager.Instance["GuestAccess_DisplayNameFormat"],
            guestName);
    }
}
