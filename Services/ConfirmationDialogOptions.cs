using System;

namespace ReversiGameSystem.Services;

/// <summary>
/// Selects localized content and actions for a reusable confirmation dialog.
/// </summary>
internal sealed class ConfirmationDialogOptions
{
    internal ConfirmationDialogOptions(
        string titleKey,
        string messageKey,
        string confirmButtonKey,
        string cancelButtonKey)
    {
        TitleKey = titleKey;
        MessageKey = messageKey;
        ConfirmButtonKey = confirmButtonKey;
        CancelButtonKey = cancelButtonKey;
    }

    internal string TitleKey { get; }

    internal string MessageKey { get; }

    internal string ConfirmButtonKey { get; }

    internal string CancelButtonKey { get; }

    internal object[] MessageArguments { get; set; } = Array.Empty<object>();
}
