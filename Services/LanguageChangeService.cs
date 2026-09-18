using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
        if (dialogs.Confirm(
            languageComboBox,
            "Common_ChangeLanguageTitle",
            "Common_ChangeLanguageMessage"))
        {
            BindingExpression? binding = languageComboBox.GetBindingExpression(
                Selector.SelectedValueProperty);
            binding?.UpdateSource();
            return;
        }

        languageComboBox.SelectedValue = currentCulture;
    }
}
