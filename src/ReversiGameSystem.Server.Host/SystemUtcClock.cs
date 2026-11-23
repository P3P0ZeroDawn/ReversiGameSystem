using System;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Host;

internal sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
