using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;
using MatchMove = ReversiGameSystem.Matches.MatchMove;

namespace ReversiGameSystem.Features.Matches;

internal sealed class MatchReplayViewModel
{
    private readonly MatchRecording _recording;

    internal MatchReplayViewModel(MatchRecording recording)
    {
        _recording = recording;
        PlayerPictures = new MatchPlayerPictures(recording.Match.Participants);
    }

    internal MatchPlayerPictures PlayerPictures { get; }

    internal int FrameIndex { get; private set; }
    internal bool IsPlaying { get; private set; }
    internal int MoveCount { get { return _recording.Moves.Length; } }
    internal int TurnDurationSeconds { get { return _recording.TurnDurationSeconds; } }
    internal bool IsFinished
    {
        get { return _recording.Match.State == ConfirmedMatchState.Finished; }
    }
    internal string FinalResult
    {
        get
        {
            LocalizationManager localization = LocalizationManager.Instance;
            RemoteMatchConclusion? conclusion = _recording.Match.Conclusion;
            string description = localization["MatchInformation_NoFinalResult"];
            if (IsFinished && conclusion != null)
            {
                description = conclusion.WinnerSeatNumber.HasValue
                    ? string.Format(localization["MatchInformation_WinnerFormat"],
                        GetPlayerName(conclusion.WinnerSeatNumber.Value))
                    : localization["MatchInformation_DrawResult"];
            }
            return description;
        }
    }

    internal string GetPlayerName(int seatNumber)
    {
        MatchParticipant? participant = _recording.Match.Participants.FirstOrDefault(
            player => player.SeatNumber == seatNumber);
        string name = participant?.DisplayName ?? string.Empty;
        if (participant?.IsSystem == true)
        {
            name = LocalizationManager.Instance["Game_SystemPlayerName"];
        }
        return name;
    }

    internal ReversiBoard ReadBoard()
    {
        return MatchBoardMapper.MapBoard(_recording.BoardFrames[FrameIndex]);
    }

    internal string[] ReadMoveDescriptions()
    {
        List<string> descriptions = new List<string>();
        foreach (MatchMove move in _recording.Moves)
        {
            descriptions.Add(string.Format(
                LocalizationManager.Instance["MatchInformation_MoveFormat"],
                move.MoveNumber, GetPlayerName(move.SeatNumber), GetCoordinate(move)));
        }
        return descriptions.ToArray();
    }

    internal MovementHistoryItem[] ReadMovementDetails()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        CultureInfo culture = CultureInfo.GetCultureInfo(localization.CultureName);
        List<MovementHistoryItem> details = new List<MovementHistoryItem>();
        foreach (MatchMove move in _recording.Moves)
        {
            details.Add(new MovementHistoryItem
            {
                SequenceText = string.Format(localization["MovementHistory_SequenceFormat"],
                    move.MoveNumber),
                DateText = string.Format(localization["MovementHistory_DateFormat"],
                    move.PlayedAtUtc.ToLocalTime().ToString("g", culture)),
                PlayerText = string.Format(localization["MovementHistory_PlayerFormat"],
                    GetPlayerName(move.SeatNumber)),
                PositionText = string.Format(localization["MovementHistory_PositionFormat"],
                    GetCoordinate(move)),
                AffectedText = string.Format(localization["MovementHistory_AffectedPiecesFormat"],
                    move.FlippedPieceCount),
                OriginText = string.Format(localization["MovementHistory_OriginFormat"],
                    localization[move.IsAutomatic ? "MovementHistory_AutomaticOrigin"
                        : "MovementHistory_ManualOrigin"])
            });
        }
        return details.ToArray();
    }

    private static string GetCoordinate(MatchMove move)
    {
        return ((char)('A' + move.Column)).ToString() + (move.Row + 1);
    }

    internal void PreviousFrame()
    {
        IsPlaying = false;
        if (FrameIndex > 0)
        {
            FrameIndex--;
        }
    }

    internal void NextFrame()
    {
        if (FrameIndex < _recording.BoardFrames.Length - 1)
        {
            FrameIndex++;
        }
        if (FrameIndex == _recording.BoardFrames.Length - 1)
        {
            IsPlaying = false;
        }
    }

    internal void TogglePlayback()
    {
        if (!IsPlaying && FrameIndex == _recording.BoardFrames.Length - 1)
        {
            FrameIndex = 0;
        }
        IsPlaying = !IsPlaying && MoveCount > 0;
    }

    internal void PausePlayback()
    {
        IsPlaying = false;
    }
}

internal sealed class MovementHistoryItem
{
    public string SequenceText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string PlayerText { get; set; } = string.Empty;
    public string PositionText { get; set; } = string.Empty;
    public string AffectedText { get; set; } = string.Empty;
    public string OriginText { get; set; } = string.Empty;
}
