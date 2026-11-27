using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ReversiGameSystem.Game;
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Features.Matches;

internal sealed class MatchPlayerPictures
{
    private const int BlackSeatNumber = 1;
    private const int WhiteSeatNumber = 2;
    private readonly MatchParticipant[] _participants;
    private bool _active = true;

    internal MatchPlayerPictures(MatchParticipant[] participants)
    {
        _participants = participants;
        foreach (MatchParticipant participant in participants)
        {
            if (participant.IsSystem)
            {
                SystemPlayerColor = participant.SeatNumber == BlackSeatNumber
                    ? DiscColor.Black : DiscColor.White;
            }
        }
    }

    internal BitmapSource? BlackPlayerPicture { get; private set; }
    internal BitmapSource? WhitePlayerPicture { get; private set; }
    internal DiscColor SystemPlayerColor { get; } = DiscColor.Empty;

    internal async Task<ProfileStatus> LoadPlayerPictures(
        PlayerProfileClient profiles, ClientSession session)
    {
        string? sessionToken = session.SessionToken;
        if (!_active || sessionToken == null || string.IsNullOrWhiteSpace(sessionToken))
        {
            return ProfileStatus.InvalidSession;
        }
        if (session.IsGuest || !UserPreferencesState.UseProfilePicturesOnPieces)
        {
            return ProfileStatus.Success;
        }
        foreach (MatchParticipant participant in _participants)
        {
            if (participant.IsSystem || !participant.AccountId.HasValue)
            {
                continue;
            }
            BitmapSource? picture = session.ProfilePicture;
            if (participant.AccountId != session.AccountId || picture == null)
            {
                ProfileStatus profileStatus = await profiles.LoadProfile(participant.AccountId);
                if (!_active || session.SessionToken != sessionToken)
                {
                    return ProfileStatus.InvalidSession;
                }
                if (profileStatus != ProfileStatus.Success)
                {
                    return profileStatus;
                }
                PlayerProfile player = profiles.ReadProfile(participant.AccountId);
                if (!player.Account.HasProfileImage)
                {
                    continue;
                }
                ProfileStatus imageStatus = await profiles.LoadProfileImage(participant.AccountId);
                if (!_active || session.SessionToken != sessionToken)
                {
                    return ProfileStatus.InvalidSession;
                }
                if (imageStatus != ProfileStatus.Success)
                {
                    return imageStatus;
                }
                PlayerImageContent confirmedImage = profiles.ReadProfileImage(participant.AccountId);
                picture = PlayerImageService.CreatePreview(confirmedImage);
            }
            if (participant.SeatNumber == BlackSeatNumber)
            {
                BlackPlayerPicture = picture;
            }
            else if (participant.SeatNumber == WhiteSeatNumber)
            {
                WhitePlayerPicture = picture;
            }
            if (participant.AccountId == session.AccountId && picture != null)
            {
                session.UpdateProfilePicture(picture);
            }
        }
        return ProfileStatus.Success;
    }

    internal void Deactivate()
    {
        _active = false;
        BlackPlayerPicture = null;
        WhitePlayerPicture = null;
    }
}
