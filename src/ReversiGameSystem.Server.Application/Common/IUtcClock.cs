using System;

namespace ReversiGameSystem.Server.Application.Common;

/// <summary>
/// Provides the authoritative current UTC instant.
/// </summary>
public interface IUtcClock
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}
