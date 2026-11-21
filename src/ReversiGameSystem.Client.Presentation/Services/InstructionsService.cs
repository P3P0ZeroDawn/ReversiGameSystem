using System;
using System.IO;
using System.Resources;

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

internal sealed class InstructionsService
{
    private const string InstructionsTitleKey = "Instructions_Title";
    private const string InstructionsRulesKey = "Instructions_RulesMessage";
    private const string MatchContextKey = "Instructions_MatchContextFormat";
    private const string ResourceUnavailableMessage = "The instruction resource is unavailable.";
    private readonly Func<string, string> _resourceLookup;

    internal InstructionsService(Func<string, string> resourceLookup)
    {
        _resourceLookup = resourceLookup;
    }

    internal InstructionsLoadResult TryLoadGeneral(out InstructionsContent? content)
    {
        content = null;
        if (!TryGetResource(InstructionsTitleKey, out string title)
            || !TryGetResource(InstructionsRulesKey, out string rules))
        {
            return InstructionsLoadResult.ResourcesError;
        }

        content = new InstructionsContent(title, rules);
        return InstructionsLoadResult.Success;
    }

    internal InstructionsContent LoadForMatch(string pieceColorName, int turnTimeSeconds)
    {
        string title = ReadResource(InstructionsTitleKey);
        string rules = ReadResource(InstructionsRulesKey);
        string contextFormat = ReadResource(MatchContextKey);
        string message = string.Format(contextFormat, pieceColorName, turnTimeSeconds, rules);
        return new InstructionsContent(title, message);
    }

    internal InstructionsLoadResult TryLoadForMatch(
        string pieceColorName,
        int turnTimeSeconds,
        out InstructionsContent? content)
    {
        InstructionsLoadResult status;
        try
        {
            content = LoadForMatch(pieceColorName, turnTimeSeconds);
            status = InstructionsLoadResult.Success;
        }
        catch (Exception exception) when (exception is FormatException
            or InvalidOperationException
            or MissingManifestResourceException
            or MissingSatelliteAssemblyException
            or IOException
            or BadImageFormatException)
        {
            content = null;
            status = InstructionsLoadResult.ResourcesError;
        }
        return status;
    }

    private string ReadResource(string key)
    {
        string value = _resourceLookup(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MissingManifestResourceException(ResourceUnavailableMessage);
        }
        return value;
    }

    private bool TryGetResource(string key, out string value)
    {
        bool isFound;
        try
        {
            value = _resourceLookup(key);
            isFound = !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception exception) when (exception is InvalidOperationException
                                            or MissingManifestResourceException
                                            or MissingSatelliteAssemblyException
                                            or IOException
                                            or BadImageFormatException)
        {
            value = string.Empty;
            isFound = false;
        }

        return isFound;
    }
}
