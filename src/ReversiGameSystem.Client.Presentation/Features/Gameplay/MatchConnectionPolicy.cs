using System;

namespace ReversiGameSystem.Features.Gameplay;

internal static class MatchConnectionPolicy
{
    internal static readonly TimeSpan ConnectionCheckTimeout = TimeSpan.FromSeconds(3);
    internal static readonly TimeSpan ConnectionCheckInterval = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan RecoveryRetryDelay = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan ReconnectionWindow = TimeSpan.FromSeconds(30);
}
