using System.Runtime.Serialization;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class AuthenticationOutcomeMapper
{
    internal static AuthenticationOutcome Map(Proxy.AuthenticatedSession? session)
    {
        if (session?.Account == null || session.Account.AccountId <= 0
            || string.IsNullOrWhiteSpace(session.SessionToken)
            || string.IsNullOrWhiteSpace(session.Account.Username)
            || string.IsNullOrWhiteSpace(session.EmailAddress)
            || session.ExpiresAtUtc <= System.DateTime.UtcNow)
        {
            throw new SerializationException("The session service returned an invalid session.");
        }
        AuthenticatedAccountSession mappedSession = new AuthenticatedAccountSession
        {
            SessionToken = session.SessionToken ?? string.Empty,
            AccountId = session.Account.AccountId,
            Username = session.Account.Username ?? string.Empty,
            EmailAddress = session.EmailAddress ?? string.Empty,
            PreferredLocaleCode = session.PreferredLocaleCode ?? string.Empty,
            ExpiresAtUtc = session.ExpiresAtUtc
        };
        return new AuthenticationOutcome(AuthenticationStatus.Success, mappedSession);
    }
}
