using System.Threading.Tasks;
using System.Windows.Controls;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Services;

internal static class LanguageChangeService
{
    private const string ConfirmationTitleKey = "Common_ChangeLanguageTitle";
    private const string ConfirmationMessageKey = "Common_ChangeLanguageMessage";
    private const string PersistenceErrorTitleKey = "LanguageSettings_PersistenceErrorTitle";
    private const string PersistenceErrorMessageKey = "LanguageSettings_PersistenceErrorMessage";
    private const string NotAvailablePrefix = "LanguageSettings_NotAvailable";
    private const string ResourcesErrorPrefix = "LanguageSettings_ResourcesError";
    private const string TitleSuffix = "Title";
    private const string MessageSuffix = "Message";

    internal static async Task ConfirmSelection(ComboBox languageComboBox)
    {
        LocalizationManager localization = LocalizationManager.Instance;
        if (localization.IsChangingCulture)
        {
            return;
        }
        string? selectedCulture = languageComboBox.SelectedValue as string;
        string currentCulture = localization.CultureName;
        if (selectedCulture == null || selectedCulture == currentCulture)
        {
            return;
        }
        DialogService dialogs = new DialogService(localization);
        if (!dialogs.Confirm(languageComboBox, ConfirmationTitleKey, ConfirmationMessageKey))
        {
            languageComboBox.SelectedValue = currentCulture;
            return;
        }

        bool applied = false;
        languageComboBox.IsEnabled = false;
        try
        {
            ClientSession session = ClientRuntime.CurrentSession;
            string? token = session.SessionToken;
            if (!session.IsGuest && token != null && !string.IsNullOrWhiteSpace(token))
            {
                PreferencesStatus status = await ClientRuntime.PreferencesService.ChangeLanguage(
                    token, selectedCulture);
                if (token != ClientRuntime.CurrentSession.SessionToken)
                {
                    return;
                }
                if (status != PreferencesStatus.Success)
                {
                    dialogs.ShowError(languageComboBox,
                        PersistenceErrorTitleKey, PersistenceErrorMessageKey);
                    return;
                }
            }

            LanguageChangeResult languageStatus = localization.TrySetCulture(selectedCulture);
            applied = languageStatus == LanguageChangeResult.Success;
            if (!applied)
            {
                string prefix = languageStatus == LanguageChangeResult.NotAvailable
                    ? NotAvailablePrefix : ResourcesErrorPrefix;
                dialogs.ShowError(languageComboBox, prefix + TitleSuffix, prefix + MessageSuffix);
            }
        }
        finally
        {
            languageComboBox.IsEnabled = true;
            if (!applied)
            {
                languageComboBox.SelectedValue = localization.CultureName;
            }
        }
    }
}
