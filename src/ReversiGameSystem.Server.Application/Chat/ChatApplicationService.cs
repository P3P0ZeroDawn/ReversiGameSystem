using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Coordinates authorization, validation, moderation, and chat history.</summary>
public class ChatApplicationService
{
    private readonly ISessionRepository _sessions;
    private readonly IChatRepository _chat;
    private readonly IUtcClock _clock;

    /// <summary>Initializes the chat application service.</summary>
    /// <param name="sessions">The authenticated session repository.</param>
    /// <param name="chat">The durable chat repository.</param>
    /// <param name="clock">The source of current UTC time.</param>
    /// <exception cref="ArgumentNullException">A dependency is absent.</exception>
    public ChatApplicationService(
        ISessionRepository sessions,
        IChatRepository chat,
        IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Evaluates whether a session may access chat for a match.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected access status.</returns>
    public ChatStatus EvaluateParticipantAccess(string sessionToken, long matchId)
    {
        SessionOperationResult<AuthenticatedSession> session =
            ResolveAuthenticatedSession(sessionToken);
        ChatStatus status = MapSessionStatus(session.Status);
        if (status == ChatStatus.Success && matchId <= 0)
        {
            status = ChatStatus.InvalidMatchId;
        }
        else if (status == ChatStatus.Success)
        {
            status = _chat.EvaluateParticipantAccess(
                session.GetValue().AccountId,
                matchId);
        }

        return status;
    }

    /// <summary>Opens authorized chat access and reports an active account mute.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected access result.</returns>
    public ChatChannelAccess PrepareChatChannel(string sessionToken, long matchId)
    {
        SessionOperationResult<AuthenticatedSession> session =
            ResolveAuthenticatedSession(sessionToken);
        if (session.Status != SessionOperationStatus.Success)
        {
            return new ChatChannelAccess(MapSessionStatus(session.Status));
        }
        if (matchId <= 0)
        {
            return new ChatChannelAccess(ChatStatus.InvalidMatchId);
        }

        return _chat.LoadChannelAccess(
            session.GetValue().AccountId,
            matchId,
            _clock.UtcNow);
    }

    /// <summary>Validates and durably processes one participant message.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <param name="content">The proposed message content.</param>
    /// <returns>The expected message result.</returns>
    public ChatMessageSubmission SubmitChatMessage(
        string sessionToken,
        long matchId,
        string content)
    {
        SessionOperationResult<AuthenticatedSession> session =
            ResolveAuthenticatedSession(sessionToken);
        if (session.Status != SessionOperationStatus.Success)
        {
            return new ChatMessageSubmission(MapSessionStatus(session.Status));
        }
        if (matchId <= 0)
        {
            return new ChatMessageSubmission(ChatStatus.InvalidMatchId);
        }

        string trimmedContent = content is null ? string.Empty : content.Trim();
        if (trimmedContent.Length == 0)
        {
            return new ChatMessageSubmission(ChatStatus.EmptyMessage);
        }
        if (trimmedContent.Length > ChatContentPolicy.MaximumMessageLength)
        {
            return new ChatMessageSubmission(ChatStatus.MessageTooLong);
        }

        return _chat.ProcessMessageSubmission(
            session.GetValue().AccountId,
            matchId,
            trimmedContent,
            _clock.UtcNow);
    }

    /// <summary>Loads the authorized durable conversation in message order.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected history result.</returns>
    public ChatHistory LoadChatHistory(string sessionToken, long matchId)
    {
        SessionOperationResult<AuthenticatedSession> session =
            ResolveAuthenticatedSession(sessionToken);
        if (session.Status != SessionOperationStatus.Success)
        {
            return new ChatHistory(MapSessionStatus(session.Status));
        }
        if (matchId <= 0)
        {
            return new ChatHistory(ChatStatus.InvalidMatchId);
        }

        return _chat.LoadMessageHistory(session.GetValue().AccountId, matchId);
    }

    /// <summary>Checks whether a channel belongs to the muted account and match.</summary>
    /// <param name="mutedSessionToken">The session that caused the mute.</param>
    /// <param name="recipientSessionToken">The candidate callback session.</param>
    /// <param name="matchId">The candidate callback match.</param>
    /// <returns>True when the callback may receive the mute.</returns>
    public bool CanReceiveMuteNotification(
        string mutedSessionToken,
        string recipientSessionToken,
        long matchId)
    {
        SessionOperationResult<AuthenticatedSession> mutedSession =
            ResolveAuthenticatedSession(mutedSessionToken);
        SessionOperationResult<AuthenticatedSession> recipientSession =
            ResolveAuthenticatedSession(recipientSessionToken);
        bool canReceive = mutedSession.Status == SessionOperationStatus.Success
            && recipientSession.Status == SessionOperationStatus.Success
            && mutedSession.GetValue().AccountId == recipientSession.GetValue().AccountId;
        if (canReceive)
        {
            canReceive = _chat.EvaluateParticipantAccess(
                recipientSession.GetValue().AccountId,
                matchId)
                == ChatStatus.Success;
        }

        return canReceive;
    }

    private SessionOperationResult<AuthenticatedSession> ResolveAuthenticatedSession(
        string sessionToken)
    {
        SessionOperationResult<AuthenticatedSession> session;
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            session = SessionOperationResult<AuthenticatedSession>.CreateFailure(
                SessionOperationStatus.MissingSessionToken);
        }
        else
        {
            session = _sessions.FindAuthenticated(sessionToken);
        }

        return session;
    }

    private static ChatStatus MapSessionStatus(SessionOperationStatus status)
    {
        ChatStatus chatStatus = ChatStatus.InvalidSession;
        if (status == SessionOperationStatus.Success)
        {
            chatStatus = ChatStatus.Success;
        }
        else if (status == SessionOperationStatus.MissingSessionToken)
        {
            chatStatus = ChatStatus.MissingSessionToken;
        }

        return chatStatus;
    }
}
