namespace ReversiGameSystem.Server.Application.Accounts;

/// <summary>Stores the active authenticated and guest sessions owned by the server.</summary>
public interface ISessionRepository
{
    /// <summary>Creates an authenticated session for a verified account.</summary>
    /// <param name="account">The verified active account.</param>
    /// <returns>The created authenticated session.</returns>
    public AuthenticatedSession Create(LoginAccount account);

    /// <summary>Atomically reserves one validated guest display name.</summary>
    /// <param name="displayName">The validated display name including its prefix.</param>
    /// <returns>The expected guest-session result.</returns>
    public SessionOperationResult<GuestSession> CreateGuest(string displayName);

    /// <summary>Finds an active authenticated session without exposing an output parameter.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <returns>The active session or an invalid-session status.</returns>
    public SessionOperationResult<AuthenticatedSession> FindAuthenticated(
        string sessionToken);

    /// <summary>Tries to retrieve an active authenticated session.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <param name="session">The active session, or null when absent.</param>
    /// <returns>True when the authenticated session exists and remains active.</returns>
    public bool TryGet(
        string sessionToken,
        out AuthenticatedSession? session);

    /// <summary>Tries to retrieve an active guest session.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    /// <param name="session">The active guest session, or null when absent.</param>
    /// <returns>True when the guest session exists and remains active.</returns>
    public bool TryGetGuest(string sessionToken, out GuestSession? session);

    /// <summary>Revokes an authenticated or guest session idempotently.</summary>
    /// <param name="sessionToken">The opaque session token.</param>
    public void Revoke(string sessionToken);
}
