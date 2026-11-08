using System;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Defines authoritative chat access and atomic moderation operations.</summary>
public interface IChatRepository
{
    /// <summary>Checks whether an active account participates in an active match.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected authorization status.</returns>
    public ChatStatus EvaluateParticipantAccess(long accountId, long matchId);

    /// <summary>Checks chat access and retrieves any active account mute.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <param name="nowUtc">The authoritative current UTC instant.</param>
    /// <returns>The expected access result.</returns>
    public ChatChannelAccess LoadChannelAccess(
        long accountId,
        long matchId,
        DateTime nowUtc);

    /// <summary>Authorizes and commits the accepted message or moderation result atomically.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <param name="content">The validated message content.</param>
    /// <param name="sentAtUtc">The authoritative send instant.</param>
    /// <returns>The persisted message or moderation result.</returns>
    public ChatMessageSubmission ProcessMessageSubmission(
        long accountId,
        long matchId,
        string content,
        DateTime sentAtUtc);

    /// <summary>Authorizes and loads the conversation in stable message order.</summary>
    /// <param name="accountId">The authenticated account identifier.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected ordered history result.</returns>
    public ChatHistory LoadMessageHistory(long accountId, long matchId);
}
