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
        _playerName = ClientRuntime.CurrentSession.Username
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
            items.Add(CreateHistoryItem(match, localization, culture));
        }

        historyItemsControl.ItemsSource = items;
        emptyHistoryTextBlock.Visibility = items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private MatchHistoryItem CreateHistoryItem(
        MatchInformation match,
        LocalizationManager localization,
        CultureInfo culture)
    {
        bool playerUsesBlackPieces = match.BlackPlayerName == _playerName;
        string opponentName = playerUsesBlackPieces
            ? match.WhitePlayerName
            : match.BlackPlayerName;
        MatchOutcomePresentation outcome = CreateOutcomePresentation(match);

        return new MatchHistoryItem
        {
            MatchId = match.Id,
            ResultText = localization[outcome.ResultKey],
            ResultSymbol = outcome.Symbol,
            ResultBackground = new SolidColorBrush(outcome.Background),
            ResultForeground = new SolidColorBrush(outcome.Foreground),
            OpponentText = string.Format(
                localization["MatchHistory_OpponentFormat"],
                opponentName),
            DateText = CreateDateText(match, localization, culture),
            StateText = CreateStateText(match, localization),
            ScoreText = CreateScoreText(match, localization, playerUsesBlackPieces),
            EndReasonText = GetEndReasonText(match, localization)
        };
    }

    private MatchOutcomePresentation CreateOutcomePresentation(MatchInformation match)
    {
        if (match.State != MatchRecordState.Finished)
        {
            return MatchOutcomePresentation.InProgress();
        }

        if (match.WinnerName == null)
        {
            return MatchOutcomePresentation.Draw();
        }

        return match.WinnerName == _playerName
            ? MatchOutcomePresentation.Victory()
            : MatchOutcomePresentation.Defeat();
    }

    private static string CreateDateText(
        MatchInformation match,
        LocalizationManager localization,
        CultureInfo culture)
    {
        return string.Format(
            localization["MatchHistory_DateFormat"],
            match.RecordedAtUtc.ToLocalTime().ToString("g", culture));
    }

    private static string CreateStateText(
        MatchInformation match,
        LocalizationManager localization)
    {
        string stateKey = match.State == MatchRecordState.Finished
            ? "MatchHistory_FinishedState"
            : "MatchHistory_InProgressState";
        return string.Format(
            localization["MatchHistory_StateFormat"],
            localization[stateKey]);
    }

    private static string CreateScoreText(
        MatchInformation match,
        LocalizationManager localization,
        bool playerUsesBlackPieces)
    {
        string scoreKey = match.State == MatchRecordState.Finished
            ? "MatchHistory_FinalScoreFormat"
            : "MatchHistory_CurrentScoreFormat";
        DiscColor playerColor = playerUsesBlackPieces
            ? DiscColor.Black
            : DiscColor.White;
        DiscColor opponentColor = playerUsesBlackPieces
            ? DiscColor.White
            : DiscColor.Black;
        return string.Format(
            localization[scoreKey],
            match.LatestBoard.Count(playerColor),
            match.LatestBoard.Count(opponentColor));
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

    private sealed class MatchOutcomePresentation
    {
        private MatchOutcomePresentation(
            string resultKey,
            string symbol,
            MatchOutcomeColors colors)
        {
            ResultKey = resultKey;
            Symbol = symbol;
            Background = colors.Background;
            Foreground = colors.Foreground;
        }

        internal string ResultKey { get; }

        internal string Symbol { get; }

        internal Color Background { get; }

        internal Color Foreground { get; }

        internal static MatchOutcomePresentation InProgress()
        {
            Color color = Color.FromRgb(46, 112, 145);
            return new MatchOutcomePresentation(
                "MatchHistory_InProgressResult",
                "…",
                new MatchOutcomeColors(color, color));
        }

        internal static MatchOutcomePresentation Draw()
        {
            return new MatchOutcomePresentation(
                "MatchHistory_DrawResult",
                "=",
                new MatchOutcomeColors(
                    Color.FromRgb(102, 122, 109),
                    Color.FromRgb(82, 104, 91)));
        }

        internal static MatchOutcomePresentation Victory()
        {
            Color color = Color.FromRgb(23, 106, 73);
            return new MatchOutcomePresentation(
                "MatchHistory_VictoryResult",
                "✓",
                new MatchOutcomeColors(color, color));
        }

        internal static MatchOutcomePresentation Defeat()
        {
            return new MatchOutcomePresentation(
                "MatchHistory_DefeatResult",
                "×",
                new MatchOutcomeColors(
                    Color.FromRgb(198, 66, 75),
                    Color.FromRgb(169, 44, 53)));
        }
    }

    private sealed class MatchOutcomeColors
    {
        internal MatchOutcomeColors(Color background, Color foreground)
        {
            Background = background;
            Foreground = foreground;
        }

        internal Color Background { get; }

        internal Color Foreground { get; }
    }
}
