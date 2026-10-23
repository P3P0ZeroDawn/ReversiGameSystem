namespace ReversiGameSystem.Services;

/// <summary>Holds the server token for the current guest session.</summary>
internal static class SessionState
{
    internal static string? GuestToken { get; set; }
}
