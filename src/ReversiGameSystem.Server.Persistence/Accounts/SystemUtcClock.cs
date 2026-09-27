using System;
using ReversiGameSystem.Server.Application.Accounts;

namespace ReversiGameSystem.Server.Persistence.Accounts;

/// <summary>
/// Represents system utc clock.
/// </summary>
public sealed class SystemUtcClock : IUtcClock
{
    /// <summary>
    /// Gets the utc now.
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
