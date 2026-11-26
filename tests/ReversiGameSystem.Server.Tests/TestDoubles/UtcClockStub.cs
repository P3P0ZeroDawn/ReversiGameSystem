using System;
using ReversiGameSystem.Server.Application.Common;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class UtcClockStub : IUtcClock
{
    internal UtcClockStub()
        : this(new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc))
    {
    }

    internal UtcClockStub(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}
