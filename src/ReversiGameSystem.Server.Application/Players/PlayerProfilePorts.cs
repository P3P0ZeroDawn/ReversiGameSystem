using System.Threading.Tasks;

namespace ReversiGameSystem.Server.Application.Players;

public interface IPlayerProfileStore
{
    Task<PlayerProfileStoreResult> GetAsync(long accountId);
}

public sealed class PlayerProfileStoreResult
{
    public PlayerProfileStoreResult(
        PlayerProfileStoreOutcome outcome,
        PlayerProfileData? profile = null)
    {
        Outcome = outcome;
        Profile = profile;
    }

    public PlayerProfileStoreOutcome Outcome { get; }

    public PlayerProfileData? Profile { get; }
}

public enum PlayerProfileStoreOutcome
{
    Found,
    NotFound,
    Unavailable
}

public sealed class PlayerProfileData
{
    public PlayerProfileData(
        long accountId,
        string username,
        string? profileImageUri,
        int wins,
        int losses,
        int draws)
    {
        AccountId = accountId;
        Username = username;
        ProfileImageUri = profileImageUri;
        Wins = wins;
        Losses = losses;
        Draws = draws;
    }

    public long AccountId { get; }

    public string Username { get; }

    public string? ProfileImageUri { get; }

    public int Wins { get; }

    public int Losses { get; }

    public int Draws { get; }
}
