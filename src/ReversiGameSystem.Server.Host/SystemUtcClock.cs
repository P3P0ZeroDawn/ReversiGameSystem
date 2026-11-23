using System;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Host;

/// <summary>
/// Supplies the host operating system UTC clock.
/// </summary>
internal sealed class SystemUtcClock : IUtcClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
