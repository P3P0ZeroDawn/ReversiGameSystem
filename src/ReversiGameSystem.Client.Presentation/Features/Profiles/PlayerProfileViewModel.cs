using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Services;
using ReversiGameSystem.Validation;

namespace ReversiGameSystem.Features.Profiles;

internal sealed class PlayerProfileViewModel : INotifyPropertyChanged
{
    private readonly PlayerProfileClient _profiles;
    private readonly ClientSession _session;
    private readonly string? _sessionToken;
    private readonly long? _targetAccountId;
    private PlayerProfile? _profile;
    private bool _active = true;

    internal PlayerProfileViewModel(PlayerProfileClient profiles, ClientSession session,
        long? targetAccountId = null)
    {
        _profiles = profiles;
        _session = session;
        _sessionToken = session.SessionToken;
        _targetAccountId = targetAccountId;
        IsOwnProfile = !targetAccountId.HasValue || targetAccountId == session.AccountId;
        if (IsOwnProfile)
        {
            Picture = session.ProfilePicture;
        }
        _session.SessionChanged += Deactivate;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsOwnProfile { get; }
    public bool IsBusy { get; private set; }
    public bool IsReady
    {
        get
        {
            return _profile != null;
        }
    }
    public bool CanEdit
    {
        get
        {
            return IsOwnProfile && IsReady && IsCurrentSession() && !IsBusy;
        }
    }
    public string Username
    {
        get
        {
            string username = _profile?.Account.Username ?? string.Empty;
            if (_profile == null && IsOwnProfile && IsCurrentSession())
            {
                username = _session.Username ?? string.Empty;
            }
            return username;
        }
    }
    public string Email
    {
        get
        {
            return IsOwnProfile && IsCurrentSession()
                ? _session.Email ?? string.Empty : string.Empty;
        }
    }
    public BitmapSource? Picture { get; private set; }
    public UsernameValidationError UsernameValidation { get; private set; }
    public ProfileStatistics Statistics
    {
        get
        {
            return _profile?.Statistics
                ?? throw new InvalidOperationException("The profile statistics are not loaded.");
        }
    }

    internal bool IsCurrentSession()
    {
        return _active && !string.IsNullOrWhiteSpace(_sessionToken)
            && !_session.IsGuest && _session.SessionToken == _sessionToken;
    }

    internal async Task<ProfileStatus> LoadProfile()
    {
        if (!IsCurrentSession())
        {
            return ProfileStatus.InvalidSession;
        }
        if (IsBusy)
        {
            return ProfileStatus.SubmissionInProgress;
        }
        IsBusy = true;
        NotifyChanged();
        try
        {
            ProfileStatus status = await _profiles.LoadProfile(_targetAccountId);
            if (!IsCurrentSession())
            {
                return ProfileStatus.InvalidSession;
            }
            if (status == ProfileStatus.Success)
            {
                _profile = _profiles.ReadProfile(_targetAccountId);
                if (IsOwnProfile)
                {
                    _session.UpdateUsername(_profile.Account.Username);
                }
                if (_profile.Account.HasProfileImage)
                {
                    status = await LoadConfirmedImage();
                }
                else
                {
                    Picture = null;
                    if (IsOwnProfile)
                    {
                        _session.UpdateProfilePicture(null);
                    }
                }
            }
            return status;
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    internal async Task<ProfileStatus> ChangeUsername(string username)
    {
        if (!IsCurrentSession())
        {
            return ProfileStatus.InvalidSession;
        }
        if (!IsOwnProfile)
        {
            return ProfileStatus.Unauthorized;
        }
        if (IsBusy)
        {
            return ProfileStatus.SubmissionInProgress;
        }
        username = username.Trim();
        UsernameValidation = UsernamePolicy.Validate(username);
        if (UsernameValidation != UsernameValidationError.None)
        {
            return ProfileStatus.InvalidUsername;
        }
        PlayerProfile profile = _profile
            ?? throw new InvalidOperationException("Load the own profile before modifying it.");
        IsBusy = true;
        NotifyChanged();
        try
        {
            ProfileStatus status = await _profiles.ChangeUsername(username);
            if (!IsCurrentSession())
            {
                return ProfileStatus.InvalidSession;
            }
            if (status == ProfileStatus.Success)
            {
                _profile = new PlayerProfile(new PlayerAccount(profile.Account.AccountId,
                    username, profile.Account.HasProfileImage), profile.Statistics);
                _session.UpdateUsername(username);
            }
            return status;
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    internal async Task<ProfileStatus> UpdateProfileImage(string imagePath)
    {
        if (!IsCurrentSession())
        {
            return ProfileStatus.InvalidSession;
        }
        if (!IsOwnProfile)
        {
            return ProfileStatus.Unauthorized;
        }
        if (IsBusy)
        {
            return ProfileStatus.SubmissionInProgress;
        }
        IsBusy = true;
        NotifyChanged();
        try
        {
            PlayerImageContent image = await PlayerImageService.ReadImageFile(imagePath);
            if (!IsCurrentSession())
            {
                return ProfileStatus.InvalidSession;
            }
            ProfileStatus status = await _profiles.UpdateProfileImage(image);
            if (!IsCurrentSession())
            {
                return ProfileStatus.InvalidSession;
            }
            if (status == ProfileStatus.Success)
            {
                status = await LoadConfirmedImage();
                if (status == ProfileStatus.Success && _profile != null)
                {
                    _profile = new PlayerProfile(new PlayerAccount(_profile.Account.AccountId,
                        _profile.Account.Username, Picture != null), _profile.Statistics);
                }
            }
            return status;
        }
        finally
        {
            IsBusy = false;
            NotifyChanged();
        }
    }

    private async Task<ProfileStatus> LoadConfirmedImage()
    {
        ProfileStatus status = await _profiles.LoadProfileImage(_targetAccountId);
        if (!IsCurrentSession())
        {
            return ProfileStatus.InvalidSession;
        }
        if (status == ProfileStatus.Success)
        {
            PlayerImageContent confirmedImage = _profiles.ReadProfileImage(_targetAccountId);
            Picture = PlayerImageService.CreatePreview(confirmedImage);
            if (IsOwnProfile)
            {
                _session.UpdateProfilePicture(Picture);
            }
        }
        return status;
    }

    internal void Deactivate()
    {
        if (!_active)
        {
            return;
        }
        _active = false;
        _session.SessionChanged -= Deactivate;
        _profile = null;
        Picture = null;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
