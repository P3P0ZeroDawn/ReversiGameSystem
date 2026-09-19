using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ReversiGameSystem.Game;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

/// <summary>
/// Displays matches that can be joined.
/// </summary>
public partial class AvailableMatchesPage : Page
{
    private readonly string? _guestName;
    private readonly DialogService _dialogService = new DialogService(LocalizationManager.Instance);
    private readonly MatchInformationService _matchService;
    private readonly AvailableMatchesService _availableMatchesService;
    private readonly JoinMatchService _joinMatchService;

    internal string? SelectedMatchId { get; private set; }

    /// <summary>
    /// Initializes the available matches page.
    /// </summary>
    public AvailableMatchesPage()
    {
        InitializeComponent();
        string playerName = AccountPreviewState.Username
            ?? LocalizationManager.Instance["Game_DefaultPlayerName"];
        _matchService = new MatchInformationService(playerName);
        _availableMatchesService = new AvailableMatchesService(playerName);
        _joinMatchService = new JoinMatchService(playerName, null);
        LoadMatches();
    }

    /// <summary>
    /// Initializes the available matches page for a guest.
    /// </summary>
    /// <param name="guestName">The name shown while the guest is in the application.</param>
    public AvailableMatchesPage(string guestName) : this()
    {
        _guestName = guestName;
        _availableMatchesService = new AvailableMatchesService(guestName);
        _joinMatchService = new JoinMatchService(guestName, guestName);
        UpdateGuestIdentity();
        guestIdentityPanel.Visibility = Visibility.Visible;
        LoadMatches();
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(_guestName == null
            ? new MainMenuPage()
            : new MainMenuPage(_guestName));
    }

    private void OnJoinWithCodeClick(object sender, RoutedEventArgs e)
    {
        string initialText = string.Empty;
        while (true)
        {
            TextInputDialogOptions options = new TextInputDialogOptions(
                "AvailableMatches_CodeDialogTitle",
                "AvailableMatches_CodeDialogMessage",
                "AvailableMatches_CodeDialogLabel")
            {
                ConfirmButtonKey = "AvailableMatches_JoinButton",
                RequireText = true,
                MaximumLength = MatchCodePolicy.Length,
                InitialText = initialText
            };

            if (!_dialogService.TryGetText(this, options, out string enteredCode))
            {
                return;
            }

            initialText = enteredCode;
            JoinMatchResult result = _joinMatchService.TryJoinByCode(
                enteredCode,
                out MatchSession? session);
            if (result == JoinMatchResult.InvalidMatchCode)
            {
                ShowJoinWarning("InvalidMatchCode");
                continue;
            }

            if (result == JoinMatchResult.MatchCodeNotAvailable)
            {
                ShowJoinWarning("MatchCodeNotAvailable");
                continue;
            }

            HandleJoinResult(result, session);
            return;
        }
    }

    private void LoadMatches()
    {
        LocalizationManager localization = LocalizationManager.Instance;
        List<AvailableMatchItem> items = new List<AvailableMatchItem>();
        AvailableMatchesRetrievalResult retrievalResult =
            _availableMatchesService.TryGetAvailableMatches(
                out IReadOnlyList<AvailableMatch> matches);
        if (retrievalResult != AvailableMatchesRetrievalResult.Success)
        {
            availableMatchesItemsControl.ItemsSource = null;
            string prefix = retrievalResult
                == AvailableMatchesRetrievalResult.ServerConnectionFailed
                ? "AvailableMatches_ServerConnectionFailed"
                : "AvailableMatches_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        foreach (AvailableMatch match in matches)
        {
            items.Add(new AvailableMatchItem
            {
                MatchId = match.Id,
                HostName = match.HostName,
                CreatedAgoText = string.Format(
                    localization["AvailableMatches_TurnTimeFormat"],
                    match.TurnTimeSeconds),
                StateText = localization["AvailableMatches_WaitingState"]
            });
        }

        availableMatchesItemsControl.ItemsSource = items;
        List<AvailableMatchItem> spectatableItems = new List<AvailableMatchItem>();
        foreach (MatchInformation match in _matchService.GetSpectatableMatches())
        {
            spectatableItems.Add(new AvailableMatchItem
            {
                MatchId = match.Id,
                HostName = match.BlackPlayerName,
                StateText = localization["MatchInformation_InProgressState"],
                SpectateVisibility = _guestName == null
                    ? Visibility.Visible
                    : Visibility.Collapsed
            });
        }

        spectatableMatchesItemsControl.ItemsSource = spectatableItems;
    }

    private void OnRefreshMatchesClick(object sender, RoutedEventArgs e)
    {
        LoadMatches();
    }

    private void OnJoinMatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button
            || button.Tag is not string matchId
            || button.DataContext is not AvailableMatchItem item)
        {
            return;
        }

        SelectedMatchId = matchId;
        ConfirmationDialogOptions options = new ConfirmationDialogOptions(
            "JoinMatch_ConfirmationTitle",
            "JoinMatch_ConfirmationMessage",
            "AvailableMatches_JoinButton",
            "Common_CancelButton")
        {
            MessageArguments = new object[] { item.HostName }
        };
        if (!_dialogService.Confirm(this, options))
        {
            return;
        }

        JoinMatchResult result = _joinMatchService.TryJoinById(
            matchId,
            out MatchSession? session);
        HandleJoinResult(result, session);
    }

    private void HandleJoinResult(JoinMatchResult result, MatchSession? session)
    {
        switch (result)
        {
            case JoinMatchResult.Success when session != null:
                NavigationService?.Navigate(new GamePage(session));
                break;
            case JoinMatchResult.NoLongerAvailable:
                ShowJoinWarning("NoLongerAvailable");
                LoadMatches();
                break;
            case JoinMatchResult.CannotJoinOwnMatch:
                ShowJoinWarning("CannotJoinOwnMatch");
                LoadMatches();
                break;
            case JoinMatchResult.PersistenceError:
                ShowJoinError("PersistenceError");
                break;
            default:
                ShowJoinError("ServerConnectionFailed");
                break;
        }
    }

    private void ShowJoinWarning(string resourceName)
    {
        _dialogService.ShowWarning(
            this,
            $"JoinMatch_{resourceName}Title",
            $"JoinMatch_{resourceName}Message");
    }

    private void ShowJoinError(string resourceName)
    {
        _dialogService.ShowError(
            this,
            $"JoinMatch_{resourceName}Title",
            $"JoinMatch_{resourceName}Message");
    }

    private void OnSpectateMatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string matchId)
        {
            return;
        }

        MatchInformationRetrievalResult result = _matchService.TryGet(
            matchId, out MatchInformation? match);
        if (result != MatchInformationRetrievalResult.Success || match == null)
        {
            string prefix = result == MatchInformationRetrievalResult.NotFound
                ? "MatchInformation_NotFound"
                : result == MatchInformationRetrievalResult.ServerConnectionFailed
                    ? "MatchInformation_ServerConnectionFailed"
                    : "MatchInformation_RetrievalError";
            _dialogService.ShowError(this, prefix + "Title", prefix + "Message");
            return;
        }

        NavigationService?.Navigate(new MatchInformationPage(
            match,
            MatchInformationMode.Spectator,
            MatchInformationReturnTarget.AvailableMatches,
            _guestName));
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string previousCulture = LocalizationManager.Instance.CultureName;
        LanguageChangeService.ConfirmSelection((ComboBox)sender);
        if (LocalizationManager.Instance.CultureName != previousCulture)
        {
            UpdateGuestIdentity();
            LoadMatches();
        }
    }

    private void UpdateGuestIdentity()
    {
        if (_guestName == null)
        {
            return;
        }

        string displayName = GuestNameFormatter.Format(_guestName);
        guestNameTextBlock.Text = displayName;
        guestNameTextBlock.ToolTip = displayName;
    }

    private sealed class AvailableMatchItem
    {
        public string MatchId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string CreatedAgoText { get; set; } = string.Empty;
        public string StateText { get; set; } = string.Empty;
        public Visibility SpectateVisibility { get; set; }
    }
}
