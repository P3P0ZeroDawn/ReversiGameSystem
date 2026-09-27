using System;

namespace ReversiGameSystem.Services;

internal enum InstructionsLoadResult
{
    Success,
    ResourcesError
}

internal sealed class InstructionsContent
{
    internal InstructionsContent(string title, string message)
    {
        Title = title;
        Message = message;
    }

    internal string Title { get; }

    internal string Message { get; }
}

/// <summary>
/// Loads complete Reversi instructions from the active language resources.
/// </summary>
internal sealed class InstructionsService
{
    private readonly Func<string, string> _resourceLookup;

    internal InstructionsService(Func<string, string> resourceLookup)
    {
        _resourceLookup = resourceLookup;
    }

    internal InstructionsLoadResult TryLoadGeneral(out InstructionsContent? content)
    {
        content = null;
        if (!TryGetResource("Instructions_Title", out string title)
            || !TryGetResource("Instructions_RulesMessage", out string rules))
        {
            return InstructionsLoadResult.ResourcesError;
        }

        content = new InstructionsContent(title, rules);
        return InstructionsLoadResult.Success;
    }

    internal InstructionsLoadResult TryLoadForMatch(
        string pieceColorName,
        int turnTimeSeconds,
        out InstructionsContent? content)
    {
        content = null;
        if (!TryGetResource("Instructions_Title", out string title)
            || !TryGetResource("Instructions_RulesMessage", out string rules)
            || !TryGetResource("Instructions_MatchContextFormat", out string contextFormat))
        {
            return InstructionsLoadResult.ResourcesError;
        }

        try
        {
            string message = string.Format(
                contextFormat,
                pieceColorName,
                turnTimeSeconds,
                rules);
            content = new InstructionsContent(title, message);
            return InstructionsLoadResult.Success;
        }
        catch (FormatException)
        {
            return InstructionsLoadResult.ResourcesError;
        }
    }

    private bool TryGetResource(string key, out string value)
    {
        try
        {
            value = _resourceLookup(key);
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (InvalidOperationException)
        {
            value = string.Empty;
            return false;
        }
        catch (System.Resources.MissingManifestResourceException)
        {
            value = string.Empty;
            return false;
        }
        catch (System.Resources.MissingSatelliteAssemblyException)
        {
            value = string.Empty;
            return false;
        }
        catch (System.IO.IOException)
        {
            value = string.Empty;
            return false;
        }
        catch (BadImageFormatException)
        {
            value = string.Empty;
            return false;
        }
    }
}
