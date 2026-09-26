using System.Windows.Controls;
using ReversiGameSystem.Localization;

namespace ReversiGameSystem.Services;

/// <summary>
/// Confirms language selection before updating the shared UI culture.
/// </summary>
internal static class LanguageChangeService
{
    internal static void ConfirmSelection(ComboBox languageComboBox)
    {
        if (LocalizationManager.Instance.IsChangingCulture)
        {
            return;
        }

        string? selectedCulture = languageComboBox.SelectedValue as string;
        string currentCulture = LocalizationManager.Instance.CultureName;
        if (selectedCulture == null || selectedCulture == currentCulture)
        {
            return;
        }

        DialogService dialogs = new DialogService(LocalizationManager.Instance);
        if (!dialogs.Confirm(
            languageComboBox,
            "Common_ChangeLanguageTitle",
            "Common_ChangeLanguageMessage"))
        {
            languageComboBox.SelectedValue = currentCulture;
            return;
        }

        LanguageChangeResult result = LocalizationManager.Instance.TrySetCulture(selectedCulture);
        if (result == LanguageChangeResult.Success)
        {
            return;
        }

        languageComboBox.SelectedValue = currentCulture;
        string resourcePrefix = result == LanguageChangeResult.NotAvailable
            ? "LanguageSettings_NotAvailable"
            : "LanguageSettings_ResourcesError";
        if (result == LanguageChangeResult.NotAvailable)
        {
            dialogs.ShowWarning(
                languageComboBox,
                resourcePrefix + "Title",
                resourcePrefix + "Message");
            return;
        }

        dialogs.ShowError(
            languageComboBox,
            resourcePrefix + "Title",
            resourcePrefix + "Message");
    }
}
