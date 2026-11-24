using System;

namespace ReversiGameSystem.Server.Application.Common;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}
