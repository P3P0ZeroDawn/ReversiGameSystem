using System;

namespace ReversiGameSystem.Game;

internal enum MatchMoveOrigin
{
    Manual,
    Timeout,
    System
}

/// <summary>
/// Records one confirmed move and whether it was selected after a timeout.
/// </summary>
internal sealed class MatchMove
{
    internal MatchMove(
        int number,
        DiscColor playerColor,
        BoardPosition position,
        MatchMoveOrigin origin,
        DateTime recordedAtUtc)
    {
        Number = number;
        PlayerColor = playerColor;
        Position = position;
        Origin = origin;
        RecordedAtUtc = recordedAtUtc;
    }

    internal int Number { get; }

    internal DiscColor PlayerColor { get; }

    internal BoardPosition Position { get; }

    internal MatchMoveOrigin Origin { get; }

    internal bool WasAutomatic => Origin != MatchMoveOrigin.Manual;

    internal DateTime RecordedAtUtc { get; }
}
