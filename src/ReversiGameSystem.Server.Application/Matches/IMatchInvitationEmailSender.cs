using System;

namespace ReversiGameSystem.Server.Application.Matches;

/// <summary>Contains the server-resolved data required for a private match invitation.</summary>
public sealed class MatchInvitationEmail
{
    /// <summary>Creates a private match invitation email.</summary>
    public MatchInvitationEmail(
        string recipientEmail,
        string creatorDisplayName,
        string matchCode)
    {
        RecipientEmail = recipientEmail
            ?? throw new ArgumentNullException(nameof(recipientEmail));
        CreatorDisplayName = creatorDisplayName
            ?? throw new ArgumentNullException(nameof(creatorDisplayName));
        MatchCode = matchCode
            ?? throw new ArgumentNullException(nameof(matchCode));
    }

    /// <summary>Gets the server-resolved recipient email address.</summary>
    public string RecipientEmail { get; }

    /// <summary>Gets the public display name of the match creator.</summary>
    public string CreatorDisplayName { get; }

    /// <summary>Gets the join code assigned to the waiting match.</summary>
    public string MatchCode { get; }
}

/// <summary>Delivers private match invitations or throws when delivery fails.</summary>
public interface IMatchInvitationEmailSender
{
    /// <summary>Sends a match invitation email synchronously.</summary>
    void Send(MatchInvitationEmail email);
}
