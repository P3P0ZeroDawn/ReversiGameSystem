using System;
using System.Resources;

namespace ReversiGameSystem.Services;

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

    internal InstructionsContent LoadGeneral()
    {
        return new InstructionsContent(ReadResource(InstructionsTitleKey),
            ReadResource(InstructionsRulesKey));
    }

    internal InstructionsContent LoadForMatch(string pieceColorName, int turnTimeSeconds)
    {
        string title = ReadResource(InstructionsTitleKey);
        string rules = ReadResource(InstructionsRulesKey);
        string contextFormat = ReadResource(MatchContextKey);
        string message = string.Format(contextFormat, pieceColorName, turnTimeSeconds, rules);
        return new InstructionsContent(title, message);
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

}
