using System;

namespace ReversiGameSystem.Services;

internal enum ActionChoiceDialogResult
{
    Cancel,
    Primary,
    Alternate
}

/// <summary>
/// Selects localized content for a dialog with two actions and cancellation.
/// </summary>
internal sealed class ActionChoiceDialogOptions
{
    internal ActionChoiceDialogOptions(
        string titleKey,
        string messageKey,
        string primaryButtonKey,
        string alternateButtonKey,
        string cancelButtonKey)
    {
        TitleKey = titleKey;
        MessageKey = messageKey;
        PrimaryButtonKey = primaryButtonKey;
        AlternateButtonKey = alternateButtonKey;
        CancelButtonKey = cancelButtonKey;
    }

    internal string TitleKey { get; }

    internal string MessageKey { get; }

    internal string PrimaryButtonKey { get; }

    internal string AlternateButtonKey { get; }

    internal string CancelButtonKey { get; }

    internal object[] MessageArguments { get; set; } = Array.Empty<object>();
}
