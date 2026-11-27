using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Friendships;

internal sealed class FriendshipViewModel : INotifyPropertyChanged
{
    private readonly IFriendshipClient _friendships;
    private readonly ClientSession _session;
    private readonly string _sessionToken;
    private readonly long? _accountId;
    private string _searchText = string.Empty;
    private bool _searching;
    private bool _active;
    private bool _loadingRequests;
    private bool _reloadRequests;
    private bool _loadingFriends;
    private bool _reloadFriends;

    internal FriendshipViewModel(IFriendshipClient friendships, ClientSession session)
    {
        _friendships = friendships ?? throw new ArgumentNullException(nameof(friendships));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _sessionToken = session.SessionToken ?? string.Empty;
        _accountId = session.AccountId;
        IsActive = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public PlayerAccount[] Players { get; private set; } = Array.Empty<PlayerAccount>();
    public FriendItem[] Friends { get; private set; } = Array.Empty<FriendItem>();
    public FriendRequestItem[] IncomingRequests { get; private set; }
        = Array.Empty<FriendRequestItem>();
    internal bool IsActive
    {
        get { return _active; }
        set
        {
            if (_active == value)
            {
                return;
            }
            _active = value;
            if (value)
            {
                _session.SessionChanged += OnSessionChanged;
            }
            else
            {
                _session.SessionChanged -= OnSessionChanged;
                ClearSessionData();
            }
        }
    }
    internal bool IsSubmitting { get; private set; }
    internal bool IsConnected
    {
        get { return IsCurrentSession() && _friendships.IsConnected; }
    }

    internal bool IsLoadingFriends
    {
        get { return _loadingFriends; }
    }

    private void OnSessionChanged()
    {
        if (!IsCurrentSession())
        {
            ClearSessionData();
        }
    }

    private void ClearSessionData()
    {
        Players = Array.Empty<PlayerAccount>();
        Friends = Array.Empty<FriendItem>();
        IncomingRequests = Array.Empty<FriendRequestItem>();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Players)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Friends)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncomingRequests)));
    }

    internal bool IsCurrentSession()
    {
        return IsActive && !_session.IsGuest && _accountId.HasValue
            && !string.IsNullOrEmpty(_sessionToken) && _session.SessionToken == _sessionToken
            && _session.AccountId == _accountId;
    }

    internal async Task<FriendshipStatus> FindPlayers(string searchText)
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        _searchText = searchText.Trim();
        if (_searching)
        {
            return FriendshipStatus.SubmissionInProgress;
        }
        _searching = true;
        FriendshipStatus status = FriendshipStatus.Success;
        string submittedSearch;
        try
        {
            do
            {
                submittedSearch = _searchText;
                if (string.IsNullOrEmpty(submittedSearch))
                {
                    Players = Array.Empty<PlayerAccount>();
                    status = FriendshipStatus.Success;
                }
                else
                {
                    status = await _friendships.FindPlayers(_sessionToken, submittedSearch);
                    if (!IsCurrentSession())
                    {
                        return FriendshipStatus.InvalidSession;
                    }
                    if (submittedSearch == _searchText && status == FriendshipStatus.Success)
                    {
                        Players = _friendships.ReadPlayers();
                    }
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Players)));
            }
            while (submittedSearch != _searchText && IsCurrentSession());
        }
        finally
        {
            _searching = false;
        }
        return status;
    }

    internal async Task<FriendshipStatus> LoadPendingRequests()
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        if (_loadingRequests)
        {
            _reloadRequests = true;
            return FriendshipStatus.SubmissionInProgress;
        }
        _loadingRequests = true;
        FriendshipStatus status;
        try
        {
            do
            {
                _reloadRequests = false;
                status = await _friendships.LoadPendingRequests(_sessionToken);
                if (!IsCurrentSession())
                {
                    return FriendshipStatus.InvalidSession;
                }
                if (status == FriendshipStatus.Success)
                {
                    RefreshConfirmedRequests();
                }
            }
            while (_reloadRequests && status == FriendshipStatus.Success);
        }
        finally
        {
            _loadingRequests = false;
        }
        return status;
    }

    internal void RefreshConfirmedRequests()
    {
        if (!IsCurrentSession())
        {
            return;
        }
        IncomingRequests = _friendships.ReadPendingRequests()
            .Where(request => request.Recipient.AccountId == _accountId)
            .Select(request => new FriendRequestItem(request)).ToArray();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncomingRequests)));
    }

    internal async Task<FriendshipStatus> LoadFriends()
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        if (_loadingFriends)
        {
            _reloadFriends = true;
            return FriendshipStatus.SubmissionInProgress;
        }
        _loadingFriends = true;
        FriendshipStatus status;
        try
        {
            do
            {
                _reloadFriends = false;
                status = await _friendships.LoadFriends(_sessionToken);
                if (!IsCurrentSession())
                {
                    return FriendshipStatus.InvalidSession;
                }
                if (status == FriendshipStatus.Success)
                {
                    RefreshConfirmedFriends();
                }
            }
            while (_reloadFriends && status == FriendshipStatus.Success);
        }
        finally
        {
            _loadingFriends = false;
        }
        return status;
    }

    internal void RefreshConfirmedFriends()
    {
        if (!IsCurrentSession())
        {
            return;
        }
        Friends = _friendships.ReadFriends().Select(friend => new FriendItem(friend)).ToArray();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Friends)));
    }

    internal async Task<FriendshipStatus> SendFriendRequest(long recipientAccountId)
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        if (IsSubmitting)
        {
            return FriendshipStatus.SubmissionInProgress;
        }
        if (recipientAccountId <= 0)
        {
            return FriendshipStatus.InvalidAccountId;
        }
        if (recipientAccountId == _accountId)
        {
            return FriendshipStatus.CannotSendRequestToSelf;
        }
        IsSubmitting = true;
        FriendshipStatus status;
        try
        {
            status = await _friendships.SendFriendRequest(_sessionToken, recipientAccountId);
            if (!IsCurrentSession())
            {
                status = FriendshipStatus.InvalidSession;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<FriendshipStatus> RespondToFriendRequest(
        PendingFriendRequest request, bool accept)
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        if (IsSubmitting)
        {
            return FriendshipStatus.SubmissionInProgress;
        }
        if (request.Recipient.AccountId != _accountId)
        {
            return FriendshipStatus.UnauthorizedRequest;
        }
        IsSubmitting = true;
        FriendshipStatus status;
        try
        {
            status = await _friendships.RespondToFriendRequest(
                _sessionToken, request.RequestId, accept);
            if (!IsCurrentSession())
            {
                status = FriendshipStatus.InvalidSession;
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }

    internal async Task<FriendshipStatus> RemoveFriend(long friendAccountId)
    {
        if (!IsCurrentSession())
        {
            return FriendshipStatus.InvalidSession;
        }
        if (IsSubmitting)
        {
            return FriendshipStatus.SubmissionInProgress;
        }
        if (friendAccountId <= 0 || friendAccountId == _accountId)
        {
            return FriendshipStatus.InvalidAccountId;
        }
        IsSubmitting = true;
        FriendshipStatus status;
        try
        {
            status = await _friendships.RemoveFriend(_sessionToken, friendAccountId);
            if (!IsCurrentSession())
            {
                status = FriendshipStatus.InvalidSession;
            }
            else if (status == FriendshipStatus.Success)
            {
                RefreshConfirmedFriends();
            }
        }
        finally
        {
            IsSubmitting = false;
        }
        return status;
    }
}

internal sealed class FriendRequestItem
{
    private const string DateFormat = "g";

    internal FriendRequestItem(PendingFriendRequest request)
    {
        Request = request;
        CultureInfo culture = CultureInfo.GetCultureInfo(LocalizationManager.Instance.CultureName);
        ReceivedAtText = request.CreatedAtUtc.ToLocalTime().ToString(DateFormat, culture);
        DateText = string.Format(culture,
            LocalizationManager.Instance["FriendRequests_DateFormat"], ReceivedAtText);
    }

    public PendingFriendRequest Request { get; }
    public string ReceivedAtText { get; }
    public string DateText { get; }
}
