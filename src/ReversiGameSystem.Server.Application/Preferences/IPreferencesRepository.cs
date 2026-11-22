namespace ReversiGameSystem.Server.Application.Preferences;

public interface IPreferencesRepository
{
    PreferencesAccess ReadPreferences(long accountId);
    PreferencesStatus SavePreferences(long accountId, PlayerPreferences preferences);
    PreferencesStatus SetBoardBackground(long accountId, string imageReference);
}
