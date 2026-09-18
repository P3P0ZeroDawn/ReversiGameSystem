using System;

namespace ReversiGameSystem.Services;

/// <summary>
/// Selects localized text and initial content for a reusable input dialog.
/// </summary>
internal sealed class TextInputDialogOptions
{
    internal TextInputDialogOptions(string titleKey, string messageKey, string labelKey)
    {
        TitleKey = titleKey;
        MessageKey = messageKey;
        LabelKey = labelKey;
    }

    internal string TitleKey { get; }

    internal string MessageKey { get; }

    internal string LabelKey { get; }

    internal string ConfirmButtonKey { get; set; } = "Common_ConfirmButton";

    internal string CancelButtonKey { get; set; } = "Common_CancelButton";

    internal string InitialText { get; set; } = string.Empty;

    internal bool RequireText { get; set; }

    internal int ExactDigitCount { get; set; }

    internal int MaximumLength { get; set; } = 128;

    internal object[] MessageArguments { get; set; } = Array.Empty<object>();
}
