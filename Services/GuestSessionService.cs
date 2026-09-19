using System;
using System.Collections.Generic;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Services;

internal enum GuestSessionCreationResult
{
    Success,
    MissingName,
    InvalidNameFormat,
    NameAlreadyInUse,
    CreationError
}

/// <summary>
/// Represents an active, nonpersistent guest identity.
/// </summary>
internal sealed class GuestSession
{
    internal GuestSession(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        StartedAtUtc = DateTime.UtcNow;
        IsActive = true;
    }

    internal Guid Id { get; }

    internal string Name { get; }

    internal DateTime StartedAtUtc { get; }

    internal bool IsActive { get; private set; }

    internal void End()
    {
        IsActive = false;
    }
}

/// <summary>
/// Keeps the guest names reserved by active prototype sessions.
/// </summary>
internal sealed class GuestSessionStore
{
    private readonly object _syncRoot = new object();
    private readonly bool _canCreateSessions;
    private readonly Dictionary<string, GuestSession> _activeSessions =
        new Dictionary<string, GuestSession>(StringComparer.OrdinalIgnoreCase);

    internal GuestSessionStore(bool canCreateSessions = true)
    {
        _canCreateSessions = canCreateSessions;
    }

    internal GuestSessionCreationResult TryCreate(
        string providedName,
        out GuestSession? session)
    {
        session = null;
        UsernameValidationError validation = GuestUsernamePolicy.Validate(providedName);
        if (validation == UsernameValidationError.Missing)
        {
            return GuestSessionCreationResult.MissingName;
        }

        if (validation != UsernameValidationError.None)
        {
            return GuestSessionCreationResult.InvalidNameFormat;
        }

        string name = GuestUsernamePolicy.Normalize(providedName);
        lock (_syncRoot)
        {
            if (!_canCreateSessions)
            {
                return GuestSessionCreationResult.CreationError;
            }

            if (_activeSessions.ContainsKey(name))
            {
                return GuestSessionCreationResult.NameAlreadyInUse;
            }

            session = new GuestSession(name);
            _activeSessions.Add(name, session);
            return GuestSessionCreationResult.Success;
        }
    }

    internal void End(GuestSession? session)
    {
        if (session == null)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_activeSessions.TryGetValue(session.Name, out GuestSession? active)
                && active.Id == session.Id)
            {
                _activeSessions.Remove(session.Name);
            }

            session.End();
        }
    }
}

/// <summary>
/// Holds the guest session used by the local interface preview.
/// </summary>
internal static class GuestSessionPreviewState
{
    private static readonly GuestSessionStore Store = new GuestSessionStore();

    internal static GuestSession? CurrentSession { get; private set; }

    internal static GuestSessionCreationResult TryStart(
        string providedName,
        out GuestSession? session)
    {
        if (CurrentSession?.IsActive == true)
        {
            session = null;
            return GuestSessionCreationResult.CreationError;
        }

        GuestSessionCreationResult result = Store.TryCreate(providedName, out session);
        if (result == GuestSessionCreationResult.Success)
        {
            CurrentSession = session;
        }

        return result;
    }

    internal static void EndCurrent()
    {
        Store.End(CurrentSession);
        CurrentSession = null;
    }
}
