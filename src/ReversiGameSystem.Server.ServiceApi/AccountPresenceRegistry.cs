using System;
using System.Collections.Generic;
using System.Threading;

namespace ReversiGameSystem.Server.ServiceApi;

/// <summary>
/// Maintains in-memory connected presence across active accounts independently of domain services.
/// </summary>
public sealed class AccountPresenceRegistry
{
    private readonly object _gate = new object();
    private readonly Dictionary<long, PresenceEntry> _registrations =
        new Dictionary<long, PresenceEntry>();
    private readonly Dictionary<string, HashSet<long>> _sessionRegistrations =
        new Dictionary<string, HashSet<long>>(StringComparer.Ordinal);
    private readonly Dictionary<long, int> _accountConnectionCounts =
        new Dictionary<long, int>();
    private long _nextRegistrationId;

    /// <summary>
    /// Occurs when an account transitions between online and offline states.
    /// </summary>
    public event Action<long, bool>? PresenceChanged;

    /// <summary>
    /// Registers an active session connection for an account.
    /// </summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>A disposable registration that unregisters the presence connection on disposal.</returns>
    /// <exception cref="ArgumentException">The session token is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The account ID is not positive.</exception>
    public IDisposable Register(string sessionToken, long accountId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ArgumentException("Session token must not be empty.", nameof(sessionToken));
        }

        if (accountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountId), "Account ID must be positive.");
        }

        bool transitionedToOnline = false;
        long registrationId;

        lock (_gate)
        {
            registrationId = Interlocked.Increment(ref _nextRegistrationId);
            PresenceEntry entry = new PresenceEntry(registrationId, sessionToken, accountId);
            _registrations[registrationId] = entry;

            if (!_sessionRegistrations.TryGetValue(sessionToken, out HashSet<long>? registrationSet))
            {
                registrationSet = new HashSet<long>();
                _sessionRegistrations[sessionToken] = registrationSet;
            }

            registrationSet.Add(registrationId);

            if (_accountConnectionCounts.TryGetValue(accountId, out int count))
            {
                _accountConnectionCounts[accountId] = count + 1;
            }
            else
            {
                _accountConnectionCounts[accountId] = 1;
                transitionedToOnline = true;
            }
        }

        if (transitionedToOnline)
        {
            PresenceChanged?.Invoke(accountId, true);
        }

        return new PresenceRegistration(this, registrationId);
    }

    /// <summary>
    /// Unregisters an active session connection by session token.
    /// </summary>
    /// <param name="sessionToken">The session token to unregister.</param>
    public void Unregister(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        bool transitionedToOffline = false;
        long accountId = 0;

        lock (_gate)
        {
            if (!_sessionRegistrations.TryGetValue(sessionToken, out HashSet<long>? registrationSet))
            {
                return;
            }

            _sessionRegistrations.Remove(sessionToken);

            foreach (long registrationId in registrationSet)
            {
                if (_registrations.TryGetValue(registrationId, out PresenceEntry? entry))
                {
                    _registrations.Remove(registrationId);
                    accountId = entry.AccountId;
                    if (_accountConnectionCounts.TryGetValue(accountId, out int count))
                    {
                        int remaining = count - 1;
                        if (remaining <= 0)
                        {
                            _accountConnectionCounts.Remove(accountId);
                            transitionedToOffline = true;
                        }
                        else
                        {
                            _accountConnectionCounts[accountId] = remaining;
                        }
                    }
                }
            }
        }

        if (transitionedToOffline && accountId > 0)
        {
            PresenceChanged?.Invoke(accountId, false);
        }
    }

    /// <summary>
    /// Checks whether an account currently holds at least one active connection.
    /// </summary>
    /// <param name="accountId">The account identifier to check.</param>
    /// <returns>True if the account is currently online; otherwise, false.</returns>
    public bool IsOnline(long accountId)
    {
        lock (_gate)
        {
            return _accountConnectionCounts.TryGetValue(accountId, out int count) && count > 0;
        }
    }

    private void UnregisterRegistration(long registrationId)
    {
        bool transitionedToOffline = false;
        long accountId = 0;

        lock (_gate)
        {
            if (!_registrations.TryGetValue(registrationId, out PresenceEntry? entry))
            {
                return;
            }

            _registrations.Remove(registrationId);
            accountId = entry.AccountId;

            if (_sessionRegistrations.TryGetValue(entry.SessionToken, out HashSet<long>? registrationSet))
            {
                registrationSet.Remove(registrationId);
                if (registrationSet.Count == 0)
                {
                    _sessionRegistrations.Remove(entry.SessionToken);
                }
            }

            if (_accountConnectionCounts.TryGetValue(accountId, out int count))
            {
                int remaining = count - 1;
                if (remaining <= 0)
                {
                    _accountConnectionCounts.Remove(accountId);
                    transitionedToOffline = true;
                }
                else
                {
                    _accountConnectionCounts[accountId] = remaining;
                }
            }
        }

        if (transitionedToOffline && accountId > 0)
        {
            PresenceChanged?.Invoke(accountId, false);
        }
    }

    private sealed class PresenceEntry
    {
        internal PresenceEntry(long registrationId, string sessionToken, long accountId)
        {
            RegistrationId = registrationId;
            SessionToken = sessionToken;
            AccountId = accountId;
        }

        internal long RegistrationId { get; }

        internal string SessionToken { get; }

        internal long AccountId { get; }
    }

    private sealed class PresenceRegistration : IDisposable
    {
        private readonly AccountPresenceRegistry _registry;
        private readonly long _registrationId;
        private int _disposed;

        internal PresenceRegistration(AccountPresenceRegistry registry, long registrationId)
        {
            _registry = registry;
            _registrationId = registrationId;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _registry.UnregisterRegistration(_registrationId);
            }
        }
    }
}
