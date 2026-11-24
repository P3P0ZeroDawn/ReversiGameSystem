namespace ReversiGameSystem.Server.Application.Players;

public interface IPlayerProfileRepository
{
    public ProfileResult<PlayerProfile> GetProfile(long accountId);

    public ProfileResult<string> GetProfileImageReference(long accountId);

    public bool IsActive(long accountId);

    public ProfileStatus ChangeUsername(
        long accountId,
        string username);

    public ProfileStatus SetProfileImage(
        long accountId,
        string imageReference);
}
