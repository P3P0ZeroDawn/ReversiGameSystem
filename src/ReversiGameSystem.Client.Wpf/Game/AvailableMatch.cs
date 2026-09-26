namespace ReversiGameSystem.Game;

internal enum AvailableMatchState
{
    Waiting,
    InProgress,
    Finished
}

/// <summary>
/// Describes the fields used to determine whether a match can be joined.
/// </summary>
internal sealed class AvailableMatch
{
    internal AvailableMatch(
        string id,
        string code,
        string hostName,
        AvailableMatchState state,
        int participantCount,
        bool usesSystemOpponent,
        int turnTimeSeconds)
    {
        Id = id;
        Code = code;
        HostName = hostName;
        State = state;
        ParticipantCount = participantCount;
        UsesSystemOpponent = usesSystemOpponent;
        TurnTimeSeconds = turnTimeSeconds;
    }

    internal string Id { get; }
    internal string Code { get; }
    internal string HostName { get; }
    internal AvailableMatchState State { get; }
    internal int ParticipantCount { get; }
    internal bool UsesSystemOpponent { get; }
    internal int TurnTimeSeconds { get; }
}
