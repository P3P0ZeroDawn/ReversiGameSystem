using System;
using System.Collections.Generic;
using System.Linq;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Matches;

namespace ReversiGameSystem.Server.Application.Matches;

public sealed class MatchSettings
{
    public int TurnDurationSeconds { get; set; } = 40;

    public bool CountsTowardStatistics { get; set; }

    public string OpponentKind { get; set; } = "Human";
}

public sealed class MatchParticipant
{
    public int SeatNumber { get; set; }

    public long AccountId { get; set; }

    public bool IsSystem { get; set; }

    public bool IsGuest { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int ConsecutiveTimeoutCount { get; set; }

    public MatchParticipant CreateSnapshot()
    {
        MatchParticipant snapshot = new MatchParticipant
        {
            SeatNumber = SeatNumber,
            AccountId = AccountId,
            IsSystem = IsSystem,
            IsGuest = IsGuest,
            DisplayName = DisplayName,
            ConsecutiveTimeoutCount = ConsecutiveTimeoutCount
        };

        return snapshot;
    }
}

public sealed class MatchMove
{
    public int MoveNumber { get; set; }

    public int SeatNumber { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }

    public DateTime PlayedAtUtc { get; set; }

    public int FlippedPieceCount { get; set; }

    public string Origin { get; set; } = "Player";

    public MatchMove CreateSnapshot()
    {
        MatchMove snapshot = new MatchMove
        {
            MoveNumber = MoveNumber,
            SeatNumber = SeatNumber,
            Row = Row,
            Column = Column,
            PlayedAtUtc = PlayedAtUtc,
            FlippedPieceCount = FlippedPieceCount,
            Origin = Origin
        };

        return snapshot;
    }
}

public sealed class MatchPlacement
{
    public MatchPlacement(long matchId, long expectedVersion, int row, int column)
    {
        MatchId = matchId;
        ExpectedVersion = expectedVersion;
        Row = row;
        Column = column;
    }

    public long MatchId { get; }

    public long ExpectedVersion { get; }

    public int Row { get; }

    public int Column { get; }
}

public sealed class Match
{
    private int? _currentTurnSeatNumber;
    private DateTime? _turnStartedAtUtc;

    public long Id { get; set; }

    public string? Code { get; set; }

    public string State { get; set; } = "Waiting";

    public int TurnDurationSeconds { get; set; }

    public bool CountsTowardStatistics { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public MatchParticipant[] Participants { get; set; } = Array.Empty<MatchParticipant>();

    public bool IsPrivate { get; set; }

    public int[] InitialBoard
    {
        get
        {
            return ReversiEngine.CreateInitialBoard();
        }
    }

    public int[] Board { get; set; } = ReversiEngine.CreateInitialBoard();

    public long MatchStateVersion { get; set; }

    public int? CurrentTurnSeatNumber
    {
        get
        {
            if (_currentTurnSeatNumber.HasValue)
            {
                return _currentTurnSeatNumber.Value;
            }

            if (State == "InProgress")
            {
                return 1;
            }

            return null;
        }
        set
        {
            _currentTurnSeatNumber = value;
        }
    }

    public DateTime? TurnStartedAtUtc
    {
        get
        {
            return _turnStartedAtUtc ?? StartedAtUtc;
        }
        set
        {
            _turnStartedAtUtc = value;
        }
    }

    public MatchMove? LastMove { get; set; }

    public int? SkippedSeatNumber { get; set; }

    public List<MatchMove> Moves { get; set; } = new List<MatchMove>();

    public string? EndReason { get; set; }

    public int? WinnerSeatNumber { get; set; }

    public int? AbandoningSeatNumber { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public object SyncRoot { get; } = new object();

    public Match CreateSnapshot()
    {
        MatchParticipant[] participants = Participants
            .Select(participant => participant.CreateSnapshot())
            .ToArray();
        List<MatchMove> moves = Moves
            .Select(move => move.CreateSnapshot())
            .ToList();
        MatchMove? lastMove = LastMove == null ? null : LastMove.CreateSnapshot();

        Match snapshot = new Match
        {
            Id = Id,
            Code = Code,
            State = State,
            TurnDurationSeconds = TurnDurationSeconds,
            CountsTowardStatistics = CountsTowardStatistics,
            StartedAtUtc = StartedAtUtc,
            Participants = participants,
            IsPrivate = IsPrivate,
            Board = (int[])Board.Clone(),
            MatchStateVersion = MatchStateVersion,
            CurrentTurnSeatNumber = CurrentTurnSeatNumber,
            TurnStartedAtUtc = TurnStartedAtUtc,
            LastMove = lastMove,
            SkippedSeatNumber = SkippedSeatNumber,
            Moves = moves,
            EndReason = EndReason,
            WinnerSeatNumber = WinnerSeatNumber,
            AbandoningSeatNumber = AbandoningSeatNumber,
            EndedAtUtc = EndedAtUtc
        };

        return snapshot;
    }
}

public sealed class MatchInvitationPreparation
{
    public MatchInvitationPreparation(Match match, string friendEmail)
    {
        Match = match ?? throw new ArgumentNullException(nameof(match));
        FriendEmail = friendEmail ?? throw new ArgumentNullException(nameof(friendEmail));
    }

    public Match Match { get; }

    public string FriendEmail { get; }
}

public enum MatchStatus
{
    Success,

    MissingSessionToken,

    InvalidSession,

    InactiveAccount,

    InvalidMatchId,

    InvalidConfiguration,

    InvalidTurnDuration,

    MatchNotFound,

    CannotJoinOwnMatch,

    InvalidMatchCode,

    MatchCodeUnavailable,

    Unauthorized,

    MatchNotWaiting,

    MatchNotInProgress,

    InvalidFriendAccountId,

    FriendAccountUnavailable,

    FriendshipNotFound,

    MissingPlacement,

    InvalidMatchStateVersion,

    StaleMatchStateVersion,

    NotPlayersTurn,

    InvalidPosition,

    SpectatingUnavailable,

    MatchFinished
}

public sealed class MatchResult<T>
{
    public MatchResult(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Status = MatchStatus.Success;
    }

    public MatchResult(MatchStatus status)
    {
        if (status == MatchStatus.Success)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                "A rejected match result cannot specify success status.");
        }

        Status = status;
        Value = default;
    }

    public MatchStatus Status { get; }

    public T? Value { get; }
}

public sealed class MatchPlayer
{
    public MatchPlayer(AuthenticatedSession session)
    {
        AccountId = session.AccountId;
        DisplayName = session.Username;
    }

    public MatchPlayer(GuestSession session)
    {
        Guest = session;
        DisplayName = session.DisplayName;
    }

    public long? AccountId { get; }
    public string DisplayName { get; }
    public GuestSession? Guest { get; }
    public bool IsGuest
    {
        get { return Guest != null; }
    }

    public static MatchResult<MatchPlayer> ResolveSession(SessionStore sessions, string token)
    {
        SessionResult<AuthenticatedSession> authenticated = sessions.FindAuthenticated(token);
        if (authenticated.Value != null)
        {
            return new MatchResult<MatchPlayer>(new MatchPlayer(authenticated.Value));
        }
        SessionResult<GuestSession> guest = sessions.FindGuest(token);
        if (guest.Value != null)
        {
            return new MatchResult<MatchPlayer>(new MatchPlayer(guest.Value));
        }
        MatchStatus status = authenticated.Status == SessionStatus.MissingSessionToken
            ? MatchStatus.MissingSessionToken : MatchStatus.InvalidSession;
        return new MatchResult<MatchPlayer>(status);
    }

    public bool IsParticipant(long matchId, MatchParticipant participant)
    {
        bool ownsSeat = !participant.IsGuest && AccountId.HasValue
            && participant.AccountId == AccountId.Value;
        if (Guest != null)
        {
            ownsSeat = participant.IsGuest && Guest.OwnsMatchSeat(matchId, participant.SeatNumber);
        }
        return ownsSeat;
    }

    public void RegisterParticipation(Match match)
    {
        if (Guest == null)
        {
            return;
        }
        MatchParticipant participant = match.Participants.Single(player => player.IsGuest
            && string.Equals(player.DisplayName, DisplayName, StringComparison.Ordinal));
        Guest.RegisterParticipation(match.Id, participant.SeatNumber);
    }
}
