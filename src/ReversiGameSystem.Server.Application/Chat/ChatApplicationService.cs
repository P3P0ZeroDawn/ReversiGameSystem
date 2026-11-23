using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Domain.Chat;

namespace ReversiGameSystem.Server.Application.Chat;

public class ChatApplicationService
{
    private const int MaximumMessagesPerWindow = 2;
    private static readonly TimeSpan MessageRateWindow = TimeSpan.FromSeconds(1);
    private readonly SessionStore _sessions;
    private readonly IChatRepository _chat;
    private readonly IUtcClock _clock;

    public ChatApplicationService(
        SessionStore sessions,
        IChatRepository chat,
        IUtcClock clock)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ChatStatus EvaluateParticipantAccess(string sessionToken, long matchId)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        ChatStatus status = MapSessionStatus(session.Status);
        if (status == ChatStatus.Success && matchId <= 0)
        {
            status = ChatStatus.InvalidMatchId;
        }
        else if (status == ChatStatus.Success && session.Value != null)
        {
            status = _chat.EvaluateParticipantAccess(
                session.Value.AccountId,
                matchId);
        }

        return status;
    }

    public ChatChannelAccess PrepareChatChannel(string sessionToken, long matchId)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        if (session.Status != SessionStatus.Success || session.Value == null)
        {
            return new ChatChannelAccess(MapSessionStatus(session.Status));
        }
        if (matchId <= 0)
        {
            return new ChatChannelAccess(ChatStatus.InvalidMatchId);
        }

        return _chat.LoadChannelAccess(
            session.Value.AccountId,
            matchId,
            _clock.UtcNow);
    }

    public ChatMessageSubmission SubmitChatMessage(
        string sessionToken,
        long matchId,
        string content)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        if (session.Status != SessionStatus.Success || session.Value == null)
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
            session.Value.AccountId,
            matchId,
            trimmedContent,
            MaximumMessagesPerWindow,
            MessageRateWindow);
    }

    public ChatHistory LoadChatHistory(string sessionToken, long matchId)
    {
        SessionResult<AuthenticatedSession> session =
            _sessions.FindAuthenticated(sessionToken);
        if (session.Status != SessionStatus.Success || session.Value == null)
        {
            return new ChatHistory(MapSessionStatus(session.Status));
        }
        if (matchId <= 0)
        {
            return new ChatHistory(ChatStatus.InvalidMatchId);
        }

        return _chat.LoadMessageHistory(session.Value.AccountId, matchId);
    }

    public bool CanReceiveMuteNotification(
        string mutedSessionToken,
        string recipientSessionToken,
        long matchId)
    {
        if (matchId <= 0)
        {
            return false;
        }

        SessionResult<AuthenticatedSession> mutedSession =
            _sessions.FindAuthenticated(mutedSessionToken);
        SessionResult<AuthenticatedSession> recipientSession =
            _sessions.FindAuthenticated(recipientSessionToken);
        bool canReceive = mutedSession.Status == SessionStatus.Success
            && mutedSession.Value != null
            && recipientSession.Status == SessionStatus.Success
            && recipientSession.Value != null
            && mutedSession.Value.AccountId == recipientSession.Value.AccountId;
        return canReceive;
    }

    private static ChatStatus MapSessionStatus(SessionStatus status)
    {
        ChatStatus chatStatus = ChatStatus.InvalidSession;
        if (status == SessionStatus.Success)
        {
            chatStatus = ChatStatus.Success;
        }
        else if (status == SessionStatus.MissingSessionToken)
        {
            chatStatus = ChatStatus.MissingSessionToken;
        }

        return chatStatus;
    }
}
