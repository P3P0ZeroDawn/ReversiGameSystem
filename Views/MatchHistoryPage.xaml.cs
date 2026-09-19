using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Lists the matches associated with the authenticated player.
/// </summary>
public partial class MatchHistoryPage : Page
{
    private readonly string _playerName;
    private readonly MatchInformationService _matchService;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);

    /// <summary>
    /// Initializes the match-history page.
    /// </summary>
    public MatchHistoryPage()
    {
        InitializeComponent();
        _playerName = AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        _matchService = new MatchInformationService(_playerName);
        LoadHistory();
    }

    private void LoadHistory()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        MatchInformationRetrievalResult retrievalResult =
            _matchService.TryGetHistory(out IReadOnlyList<MatchInformation> matches);
        if (retrievalResult != MatchInformationRetrievalResult.Success)
        {
            historyItemsControl.ItemsSource = null;
            emptyHistoryTextBlock.Visibility = Visibility.Collapsed;
            string prefix = retrievalResult
                == MatchInformationRetrievalResult.ServerConnectionFailed
                    ? "MatchHistory_ServerConnectionFailed"
                    : "MatchHistory_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        List<MatchHistoryItem> items = new List<MatchHistoryItem>();
        CultureInfo culture = CultureInfo.GetCultureInfo(localization.CultureName);
        foreach (MatchInformation match in matches)
        {
            string opponentName = match.BlackPlayerName == _playerName
                ? match.WhitePlayerName
                : match.BlackPlayerName;
            bool playerUsesBlackPieces = match.BlackPlayerName == _playerName;
            bool isFinished = match.State == MatchRecordState.Finished;
            bool isDraw = isFinished && match.WinnerName == null;
            bool isVictory = isFinished && match.WinnerName == _playerName;
            string resultKey = !isFinished
                ? "MatchHistory_InProgressResult"
                : isDraw
                    ? "MatchHistory_DrawResult"
                    : isVictory
                        ? "MatchHistory_VictoryResult"
                        : "MatchHistory_DefeatResult";
            ReversiBoard board = match.LatestBoard;
            items.Add(new MatchHistoryItem
            {
                MatchId = match.Id,
                ResultText = localization[resultKey],
                ResultSymbol = !isFinished ? "…" : isDraw ? "=" : isVictory ? "✓" : "×",
                ResultBackground = new SolidColorBrush(!isFinished
                    ? Color.FromRgb(46, 112, 145)
                    : isDraw
                    ? Color.FromRgb(102, 122, 109)
                    : isVictory ? Color.FromRgb(23, 106, 73) : Color.FromRgb(198, 66, 75)),
                ResultForeground = new SolidColorBrush(!isFinished
                    ? Color.FromRgb(46, 112, 145)
                    : isVictory
                    ? Color.FromRgb(23, 106, 73)
                    : isDraw ? Color.FromRgb(82, 104, 91) : Color.FromRgb(169, 44, 53)),
                OpponentText = string.Format(
                    localization["MatchHistory_OpponentFormat"], opponentName),
                DateText = string.Format(
                    localization["MatchHistory_DateFormat"],
                    match.RecordedAtUtc.ToLocalTime().ToString("g", culture)),
                StateText = string.Format(
                    localization["MatchHistory_StateFormat"],
                    localization[isFinished
                        ? "MatchHistory_FinishedState"
                        : "MatchHistory_InProgressState"]),
                ScoreText = string.Format(
                    localization[isFinished
                        ? "MatchHistory_FinalScoreFormat"
                        : "MatchHistory_CurrentScoreFormat"],
                    board.Count(playerUsesBlackPieces
                        ? DiscColor.Black
                        : DiscColor.White),
                    board.Count(playerUsesBlackPieces
                        ? DiscColor.White
                        : DiscColor.Black)),
                EndReasonText = GetEndReasonText(match, localization)
            });
        }

        historyItemsControl.ItemsSource = items;
        emptyHistoryTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string GetEndReasonText(
        MatchInformation match,
        LocalizationManager localization)
    {
        if (!match.EndReason.HasValue)
        {
            return string.Empty;
        }

        string reasonKey = match.EndReason.Value == MatchEndReason.Abandonment
            ? "MatchHistory_AbandonmentReason"
            : match.EndReason.Value == MatchEndReason.Inactivity
                ? "MatchHistory_InactivityReason"
                : "MatchHistory_BoardCompletedReason";
        return string.Format(
            localization["MatchHistory_EndReasonFormat"],
            localization[reasonKey]);
    }

    private void OnViewMatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string matchId)
        {
            return;
        }

        MatchInformationRetrievalResult result = _matchService.TryGet(
            matchId, out MatchInformation? match);
        if (!HandleRetrievalResult(result) || match == null)
        {
            return;
        }

        NavigationService?.Navigate(new MatchInformationPage(
            match,
            MatchInformationMode.Replay,
            MatchInformationReturnTarget.History));
    }

    private bool HandleRetrievalResult(MatchInformationRetrievalResult result)
    {
        if (result == MatchInformationRetrievalResult.Success)
        {
            return true;
        }

        string prefix = result == MatchInformationRetrievalResult.NotFound
            ? "MatchInformation_NotFound"
            : result == MatchInformationRetrievalResult.ServerConnectionFailed
                ? "MatchInformation_ServerConnectionFailed"
                : "MatchInformation_RetrievalError";
        _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
        return false;
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new MainMenuPage());
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            LoadHistory();
        }
    }

    private sealed class MatchHistoryItem
    {
        public string MatchId { get; set; } = string.Empty;
        public string ResultText { get; set; } = string.Empty;
        public string ResultSymbol { get; set; } = string.Empty;
        public Brush ResultBackground { get; set; } = Brushes.Transparent;
        public Brush ResultForeground { get; set; } = Brushes.Black;
        public string OpponentText { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string StateText { get; set; } = string.Empty;
        public string ScoreText { get; set; } = string.Empty;
        public string EndReasonText { get; set; } = string.Empty;
    }
}
