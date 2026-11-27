using System;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Matches;

namespace ReversiGameSystem.Features.Matches;

internal sealed class MatchHistoryItem
{
    private const int BlackSeatNumber = 1;

    internal MatchHistoryItem(MatchRecord match)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        CultureInfo culture = CultureInfo.GetCultureInfo(localization.CultureName);
        MatchId = match.MatchId;
        bool finished = match.State == ConfirmedMatchState.Finished;
        int? winner = match.Conclusion?.WinnerSeatNumber;
        string resultKey = "MatchHistory_InProgressResult";
        ResultSymbol = "…";
        ResultBackground = Brushes.SteelBlue;
        if (finished)
        {
            resultKey = "MatchHistory_DrawResult";
            ResultSymbol = "=";
            ResultBackground = Brushes.SlateGray;
            if (winner.HasValue)
            {
                bool victory = winner == match.AssignedSeatNumber;
                resultKey = victory ? "MatchHistory_VictoryResult" : "MatchHistory_DefeatResult";
                ResultSymbol = victory ? "✓" : "×";
                ResultBackground = victory ? Brushes.SeaGreen : Brushes.Firebrick;
            }
        }
        ResultForeground = ResultBackground;
        ResultText = localization[resultKey];
        MatchParticipant? opponent = match.Participants.FirstOrDefault(
            participant => participant.SeatNumber != match.AssignedSeatNumber);
        string opponentName = opponent?.DisplayName ?? string.Empty;
        if (opponent?.IsSystem == true)
        {
            opponentName = localization["Game_SystemPlayerName"];
        }
        if (opponent?.IsGuest == true)
        {
            opponentName = GuestNameFormatter.Format(opponent.DisplayName);
        }
        OpponentText = string.Format(localization["MatchHistory_OpponentFormat"], opponentName);
        DateText = string.Format(localization["MatchHistory_DateFormat"],
            match.RecordedAtUtc.ToLocalTime().ToString("g", culture));
        StateText = string.Format(localization["MatchHistory_StateFormat"],
            localization[finished ? "MatchHistory_FinishedState"
                : "MatchHistory_InProgressState"]);
        bool black = match.AssignedSeatNumber == BlackSeatNumber;
        ScoreText = string.Format(localization[finished ? "MatchHistory_FinalScoreFormat"
            : "MatchHistory_CurrentScoreFormat"], black ? match.BlackPieceCount : match.WhitePieceCount,
            black ? match.WhitePieceCount : match.BlackPieceCount);
        EndReasonText = string.Empty;
        if (match.Conclusion != null)
        {
            string reasonKey = match.Conclusion.Reason switch
            {
                RemoteMatchEndReason.Abandonment => "MatchHistory_AbandonmentReason",
                RemoteMatchEndReason.Inactivity => "MatchHistory_InactivityReason",
                RemoteMatchEndReason.NoLegalMoves => "MatchHistory_BoardCompletedReason",
                _ => throw new ArgumentOutOfRangeException(nameof(match))
            };
            EndReasonText = string.Format(localization["MatchHistory_EndReasonFormat"],
                localization[reasonKey]);
        }
    }

    public long MatchId { get; }
    public string ResultText { get; }
    public string ResultSymbol { get; }
    public Brush ResultBackground { get; }
    public Brush ResultForeground { get; }
    public string OpponentText { get; }
    public string DateText { get; }
    public string StateText { get; }
    public string ScoreText { get; }
    public string EndReasonText { get; }
}
