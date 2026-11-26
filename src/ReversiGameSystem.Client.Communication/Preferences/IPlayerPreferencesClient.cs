using System.Threading.Tasks;

namespace ReversiGameSystem.Services;

internal interface IPlayerPreferencesClient
{
    Task<PreferencesStatus> LoadPreferences(string sessionToken);
    PlayerPreferences ReadPreferences();
    Task<PreferencesStatus> UpdatePreferences(string sessionToken, PlayerPreferences preferences);
    Task<PreferencesStatus> LoadBoardBackground(string sessionToken);
    bool HasBoardBackground { get; }
    PlayerImageContent ReadBoardBackground();
    Task<PreferencesStatus> ChangeBoardBackground(string sessionToken, PlayerImageContent image);
    Task<PreferencesStatus> ChangeLanguage(string sessionToken, string localeCode);
    void DiscardSession();
}
