using System.Runtime.Serialization;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Services;

internal static class AuthenticationOutcomeMapper
{
    internal static AuthenticationOutcome Map(Proxy.AuthenticationOutcome? outcome)
    {
        if (outcome == null)
        {
            throw new SerializationException(
                "The account service returned an empty authentication outcome.");
        }

        return outcome.Status switch
        {
            Proxy.AuthenticationStatus.Success => MapSuccessfulOutcome(outcome.Session),
            Proxy.AuthenticationStatus.InvalidCredentials =>
                new AuthenticationOutcome(AuthenticationStatus.InvalidCredentials),
            Proxy.AuthenticationStatus.InactiveAccount =>
                new AuthenticationOutcome(AuthenticationStatus.InactiveAccount),
            Proxy.AuthenticationStatus.PersistenceUnavailable =>
                new AuthenticationOutcome(AuthenticationStatus.ServiceUnavailable),
            _ => throw new SerializationException(
                "The account service returned an unknown authentication status.")
        };
    }

    private static AuthenticationOutcome MapSuccessfulOutcome(
        Proxy.AuthenticatedSession? session)
    {
        if (session?.Account == null)
        {
            throw new SerializationException(
                "A successful authentication outcome did not include a session.");
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
