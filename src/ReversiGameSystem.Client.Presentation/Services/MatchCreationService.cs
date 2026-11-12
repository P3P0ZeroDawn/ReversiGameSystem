using System;
using System.Collections.Generic;
using ReversiGameSystem.Game;

namespace ReversiGameSystem.Services;

internal enum MatchCreationResult
{
    Success,
    ServerConnectionFailed,
    PersistenceError
}

internal enum MatchState
{
    Waiting,
    InProgress,
    Finished,
    Cancelled
}

/// <summary>
/// Carries the client-visible state for a match created during this session.
/// </summary>
internal sealed class MatchSession
{
    private readonly List<MatchMove> _moves = new List<MatchMove>();
    private int _blackConsecutiveTimeouts;
    private int _whiteConsecutiveTimeouts;

    internal MatchSession(
        string code,
        int turnTimeSeconds,
        string creatorName,
        string? guestName,
        DiscColor localPlayerColor = DiscColor.Black,
        ReversiBoard? initialBoard = null)
    {
        Code = code;
        TurnTimeSeconds = turnTimeSeconds;
        CreatorName = creatorName;
        GuestName = guestName;
        LocalPlayerColor = localPlayerColor;
        Board = initialBoard ?? ReversiBoard.CreateInitial();
        CurrentTurnColor = DiscColor.Black;
        State = MatchState.Waiting;
    }

    internal string Code { get; }

    internal int TurnTimeSeconds { get; private set; }

    internal string CreatorName { get; }

    internal string? GuestName { get; }

    internal DiscColor LocalPlayerColor { get; }

    internal string LocalPlayerName
    {
        get
        {
            return LocalPlayerColor == DiscColor.Black
                ? CreatorName
                : OpponentName ?? string.Empty;
        }
    }

    internal string? OpponentName { get; private set; }

    internal string? WinnerName { get; private set; }

    internal DiscColor? AbandonedByColor { get; private set; }

    internal string? AbandonedByName { get; private set; }

    internal bool UsesSystemOpponent { get; private set; }

    internal ReversiBoard Board { get; private set; }

    internal DiscColor CurrentTurnColor { get; private set; }

    internal IReadOnlyList<MatchMove> Moves
    {
        get { return _moves; }
    }

    internal MatchEndReason? EndReason { get; private set; }

    internal DateTime? FinishedAtUtc { get; private set; }

    internal MatchState State { get; private set; }

    internal void ApplyTurnTime(int turnTimeSeconds)
    {
        TurnTimeSeconds = turnTimeSeconds;
    }

    internal string GetPlayerName(DiscColor color)
    {
        return color == DiscColor.Black
            ? CreatorName
            : OpponentName ?? string.Empty;
    }

    internal int GetConsecutiveTimeouts(DiscColor color)
    {
        return color == DiscColor.Black
            ? _blackConsecutiveTimeouts
            : _whiteConsecutiveTimeouts;
    }

    internal int RegisterTurnTimeout(DiscColor color)
    {
        if (color == DiscColor.Black)
        {
            _blackConsecutiveTimeouts++;
            return _blackConsecutiveTimeouts;
        }

        _whiteConsecutiveTimeouts++;
        return _whiteConsecutiveTimeouts;
    }

    internal void CommitMove(ReversiBoard board, MatchMove move, bool resetTimeouts)
    {
        Board = board;
        _moves.Add(move);
        if (!resetTimeouts)
        {
            return;
        }

        if (move.PlayerColor == DiscColor.Black)
        {
            _blackConsecutiveTimeouts = 0;
        }
        else
        {
            _whiteConsecutiveTimeouts = 0;
        }
    }

    internal void SetTurn(DiscColor color)
    {
        CurrentTurnColor = color;
    }

    internal void Finish(DiscColor? winnerColor, MatchEndReason reason)
    {
        if (State != MatchState.InProgress)
        {
            return;
        }

        WinnerName = winnerColor.HasValue
            ? GetPlayerName(winnerColor.Value)
            : null;
        EndReason = reason;
        FinishedAtUtc = DateTime.UtcNow;
        State = MatchState.Finished;
        MatchCreationService.InvalidateCode(Code);
    }

    internal void StartAgainstSystem(string systemName)
    {
        if (State != MatchState.Waiting)
        {
            return;
        }

        OpponentName = systemName;
        UsesSystemOpponent = true;
        State = MatchState.InProgress;
        MatchCreationService.InvalidateCode(Code);
    }

    internal void JoinAsSecondPlayer(string playerName)
    {
        if (State != MatchState.Waiting)
        {
            return;
        }

        OpponentName = playerName;
        State = MatchState.InProgress;
        MatchCreationService.InvalidateCode(Code);
    }

    internal void Cancel()
    {
        if (State == MatchState.Cancelled)
        {
            return;
        }

        State = MatchState.Cancelled;
        MatchCreationService.InvalidateCode(Code);
    }

    internal bool TryRegisterAbandonment(DiscColor abandoningColor)
    {
        if (State != MatchState.InProgress)
        {
            return false;
        }

        if (abandoningColor != DiscColor.Black
            && abandoningColor != DiscColor.White)
        {
            return false;
        }

        string abandoningPlayerName = GetPlayerName(abandoningColor);
        DiscColor winnerColor = ReversiBoard.GetOpponent(abandoningColor);
        if (string.IsNullOrWhiteSpace(abandoningPlayerName)
            || string.IsNullOrWhiteSpace(GetPlayerName(winnerColor)))
        {
            return false;
        }

        AbandonedByColor = abandoningColor;
        AbandonedByName = abandoningPlayerName;
        Finish(winnerColor, MatchEndReason.Abandonment);
        return State == MatchState.Finished;
    }
}

/// <summary>
/// Creates temporary client sessions until the match server replaces this boundary.
/// </summary>
internal sealed class MatchCreationService
{
    private static readonly object CodeLock = new object();
    private static readonly HashSet<string> ActiveCodes =
        new HashSet<string>(StringComparer.Ordinal);
    private static readonly Random CodeRandom = new Random();

    internal MatchCreationResult TryCreate(
        int turnTimeSeconds,
        string creatorName,
        string? guestName,
        out MatchSession? session)
    {
        session = null;
        if (turnTimeSeconds < TurnTimePolicy.MinimumSeconds
            || turnTimeSeconds > TurnTimePolicy.MaximumSeconds)
        {
            return MatchCreationResult.PersistenceError;
        }

        try
        {
            session = new MatchSession(
                GenerateUniqueCode(),
                turnTimeSeconds,
                creatorName,
                guestName);
            return MatchCreationResult.Success;
        }
        catch (InvalidOperationException)
        {
            return MatchCreationResult.PersistenceError;
        }
    }

    internal static void InvalidateCode(string code)
    {
        lock (CodeLock)
        {
            ActiveCodes.Remove(code);
        }
    }

    private static string GenerateUniqueCode()
    {
        lock (CodeLock)
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                char[] characters = new char[6];
                for (int index = 0; index < characters.Length; index++)
                {
                    characters[index] = MatchCodePolicy.AllowedCharacters[
                        CodeRandom.Next(MatchCodePolicy.AllowedCharacters.Length)];
                }

                string code = new string(characters);
                if (ActiveCodes.Add(code))
                {
                    return code;
                }
            }
        }

        throw new InvalidOperationException(
            "A unique temporary match code could not be generated.");
    }
}
