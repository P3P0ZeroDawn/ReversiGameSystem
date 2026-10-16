using System;

namespace ReversiGameSystem.Services;

internal sealed class AuthenticatedAccountSession
{
    internal string SessionToken { get; set; } = string.Empty;

    internal long AccountId { get; set; }

    internal string Username { get; set; } = string.Empty;

    internal string EmailAddress { get; set; } = string.Empty;

    internal string PreferredLocaleCode { get; set; } = string.Empty;

    internal DateTime ExpiresAtUtc { get; set; }
}
