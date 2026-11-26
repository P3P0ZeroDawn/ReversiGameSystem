using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Features.Preferences;

internal sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IPlayerPreferencesClient _client;
    private readonly ClientSession _session;
    private readonly string? _sessionToken;
    private readonly long? _accountId;
    private readonly bool _isGuest;
    private PlayerPreferences _preferences;

    internal SettingsViewModel(IPlayerPreferencesClient client, ClientSession session)
    {
        _client = client;
        _session = session;
        _sessionToken = session.SessionToken;
        _accountId = session.AccountId;
        _isGuest = session.IsGuest;
        _preferences = new PlayerPreferences
        {
            PreferredLocaleCode = Localization.LocalizationManager.Instance.CultureName,
            PieceStyleCode = UserPreferencesState.UseProfilePicturesOnPieces
                ? PlayerPreferences.ProfilePicturesPieceStyle
                    : PlayerPreferences.StandardPieceStyle,
            UseCustomBoardBackground = UserPreferencesState.UseCustomBoardBackground
        };
        BackgroundPreview = UserPreferencesState.BoardBackgroundPreview;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading { get; private set; }

    public bool IsSaving { get; private set; }

    public bool IsLoaded { get; private set; }

    public bool CanEdit
    {
        get { return IsLoaded && !IsLoading && !IsSaving && IsCurrentSession(); }
    }

    public bool UseProfilePicturesOnPieces
    {
        get { return _preferences.PieceStyleCode == PlayerPreferences.ProfilePicturesPieceStyle; }
    }

    public bool UseCustomBoardBackground
    {
        get { return _preferences.UseCustomBoardBackground; }
    }

    public BitmapSource? BackgroundPreview { get; private set; }

    internal async Task<PreferencesStatus> LoadPreferences()
    {
        if (!IsCurrentSession())
        {
            return PreferencesStatus.InvalidSession;
        }
        if (IsLoading || IsSaving)
        {
            return PreferencesStatus.OperationInProgress;
        }
        IsLoading = true;
        NotifyState();
        try
        {
            if (!IsAuthenticated())
            {
                IsLoaded = true;
                return PreferencesStatus.Success;
            }
            PreferencesStatus status = await _client.LoadPreferences(_sessionToken!);
            if (status != PreferencesStatus.Success || !IsCurrentSession())
            {
                return CurrentStatus(status);
            }
            status = await _client.LoadBoardBackground(_sessionToken!);
            if (status != PreferencesStatus.Success || !IsCurrentSession())
            {
                return CurrentStatus(status);
            }
            ApplyConfirmedBackground();
            ApplyConfirmedPreferences();
            IsLoaded = true;
            return PreferencesStatus.Success;
        }
        finally
        {
            IsLoading = false;
            NotifyState();
        }
    }

    internal Task<PreferencesStatus> ChangePieceStyle(bool enabled)
    {
        PlayerPreferences next = CopyPreferences();
        next.PieceStyleCode = enabled
            ? PlayerPreferences.ProfilePicturesPieceStyle : PlayerPreferences.StandardPieceStyle;
        return SavePreferences(next, false);
    }

    internal Task<PreferencesStatus> ChangeCustomBackground(bool enabled)
    {
        PlayerPreferences next = CopyPreferences();
        next.UseCustomBoardBackground = enabled;
        return SavePreferences(next, false);
    }

    internal Task<PreferencesStatus> ChangeLanguage(string localeCode)
    {
        PlayerPreferences next = CopyPreferences();
        next.PreferredLocaleCode = localeCode;
        return SavePreferences(next, true);
    }

    internal async Task<PreferencesStatus> ChangeBoardBackground(PlayerImageContent image)
    {
        if (!IsCurrentSession())
        {
            return PreferencesStatus.InvalidSession;
        }
        if (IsLoading || IsSaving)
        {
            return PreferencesStatus.OperationInProgress;
        }
        if (!IsLoaded)
        {
            return PreferencesStatus.MissingPreferences;
        }
        IsSaving = true;
        NotifyState();
        try
        {
            if (!IsAuthenticated())
            {
                BitmapSource? preview = PlayerImageService.CreatePreview(
                    image, PlayerImagePolicy.MaximumBackgroundOutputDimension);
                if (preview == null)
                {
                    return PreferencesStatus.InvalidImage;
                }
                if (!IsCurrentSession())
                {
                    return PreferencesStatus.InvalidSession;
                }
                UserPreferencesState.SetBoardBackground(preview);
                BackgroundPreview = preview;
                _preferences.UseCustomBoardBackground = true;
                UserPreferencesState.ApplyPreferences(_preferences);
                return PreferencesStatus.Success;
            }
            PreferencesStatus status = await _client.ChangeBoardBackground(_sessionToken!, image);
            if (status != PreferencesStatus.Success || !IsCurrentSession())
            {
                return CurrentStatus(status);
            }
            ApplyConfirmedBackground();
            ApplyConfirmedPreferences();
            return PreferencesStatus.Success;
        }
        finally
        {
            IsSaving = false;
            NotifyState();
        }
    }

    private async Task<PreferencesStatus> SavePreferences(PlayerPreferences next,
        bool languageChange)
    {
        if (!IsCurrentSession())
        {
            return PreferencesStatus.InvalidSession;
        }
        if (IsLoading || IsSaving)
        {
            return PreferencesStatus.OperationInProgress;
        }
        if (!IsLoaded && !languageChange)
        {
            return PreferencesStatus.MissingPreferences;
        }
        if (IsLoaded && next.PieceStyleCode == _preferences.PieceStyleCode
            && next.UseCustomBoardBackground == _preferences.UseCustomBoardBackground
            && next.PreferredLocaleCode == _preferences.PreferredLocaleCode)
        {
            return PreferencesStatus.Success;
        }
        IsSaving = true;
        NotifyState();
        try
        {
            if (IsAuthenticated())
            {
                PreferencesStatus status;
                if (languageChange)
                {
                    status = await _client.ChangeLanguage(_sessionToken!, next.PreferredLocaleCode);
                }
                else
                {
                    status = await _client.UpdatePreferences(_sessionToken!, next);
                }
                if (status != PreferencesStatus.Success || !IsCurrentSession())
                {
                    return CurrentStatus(status);
                }
                ApplyConfirmedPreferences();
            }
            else if (IsCurrentSession())
            {
                _preferences = next;
                UserPreferencesState.ApplyPreferences(_preferences);
            }
            return CurrentStatus(PreferencesStatus.Success);
        }
        finally
        {
            IsSaving = false;
            NotifyState();
        }
    }

    private void ApplyConfirmedPreferences()
    {
        if (!IsCurrentSession())
        {
            return;
        }
        _preferences = _client.ReadPreferences();
        UserPreferencesState.ApplyPreferences(_preferences);
    }

    private void ApplyConfirmedBackground()
    {
        if (!IsCurrentSession())
        {
            return;
        }
        if (_client.HasBoardBackground)
        {
            BitmapSource? preview = PlayerImageService.CreatePreview(
                _client.ReadBoardBackground(), PlayerImagePolicy.MaximumBackgroundOutputDimension);
            if (preview == null)
            {
                throw new InvalidOperationException("The confirmed background is unavailable.");
            }
            UserPreferencesState.SetBoardBackground(preview);
            BackgroundPreview = preview;
        }
        else
        {
            UserPreferencesState.ClearBoardBackground();
            BackgroundPreview = null;
        }
    }

    private PlayerPreferences CopyPreferences()
    {
        return new PlayerPreferences
        {
            PreferredLocaleCode = _preferences.PreferredLocaleCode,
            TimeZoneId = _preferences.TimeZoneId,
            PieceStyleCode = _preferences.PieceStyleCode,
            UseCustomBoardBackground = _preferences.UseCustomBoardBackground
        };
    }

    private bool IsAuthenticated()
    {
        return !_isGuest && _accountId.HasValue && !string.IsNullOrWhiteSpace(_sessionToken);
    }

    private bool IsCurrentSession()
    {
        return _session.SessionToken == _sessionToken && _session.AccountId == _accountId
            && _session.IsGuest == _isGuest;
    }

    private PreferencesStatus CurrentStatus(PreferencesStatus status)
    {
        return IsCurrentSession() ? status : PreferencesStatus.InvalidSession;
    }

    private void NotifyState()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
