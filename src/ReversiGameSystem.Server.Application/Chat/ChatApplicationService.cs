using System;
using ReversiGameSystem.Server.Application.Accounts;
using ReversiGameSystem.Server.Application.Matches;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Coordinates authorization, validation, moderation, and chat history.</summary>
public sealed class ChatApplicationService
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

    /// <summary>Checks whether a session can keep an open channel for a match.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected authorization status.</returns>
    public ChatOperationStatus Authorize(string sessionToken, long matchId)
    {
        ChatOperationResult<long> playerResult = GetPlayerAccountId(sessionToken);
        if (playerResult.Status != ChatOperationStatus.Success)
        {
            return playerResult.Status;
        }
        if (matchId <= 0)
        {
            return ChatOperationStatus.InvalidMatchId;
        }

        ChatOperationStatus status = ChatOperationStatus.Success;
        try
        {
            _chat.Authorize(playerResult.GetValue(), matchId);
        }
        catch (MatchRejectedException exception)
        {
            status = GetStatus(exception.Reason);
        }

        return status;
    }

    /// <summary>Opens authorized chat access and reports an active account mute.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected channel access result.</returns>
    public ChatOpenResult Open(string sessionToken, long matchId)
    {
        ChatOperationResult<long> playerResult = GetPlayerAccountId(sessionToken);
        if (playerResult.Status != ChatOperationStatus.Success)
        {
            return ChatOpenResult.CreateFailure(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return ChatOpenResult.CreateFailure(
                ChatOperationStatus.InvalidMatchId);
        }

        ChatOpenResult result;
        long accountId = playerResult.GetValue();
        try
        {
            _chat.Authorize(accountId, matchId);
            ChatMute? activeMute = null;
            if (_chat.TryGetMute(accountId, _clock.UtcNow, out ChatMute? mute)
                && mute != null)
            {
                activeMute = mute;
            }

            result = ChatOpenResult.CreateSuccess(activeMute);
        }
        catch (MatchRejectedException exception)
        {
            result = ChatOpenResult.CreateFailure(
                GetStatus(exception.Reason));
        }

        return result;
    }

    /// <summary>Validates and durably processes one participant message.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <param name="content">The proposed message content.</param>
    /// <returns>The expected message operation result.</returns>
    public ChatOperationResult<ChatMessage> Send(
        string sessionToken,
        long matchId,
        string content)
    {
        ChatOperationResult<long> playerResult = GetPlayerAccountId(sessionToken);
        if (playerResult.Status != ChatOperationStatus.Success)
        {
            return ChatOperationResult<ChatMessage>.CreateFailure(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return ChatOperationResult<ChatMessage>.CreateFailure(
                ChatOperationStatus.InvalidMatchId);
        }

        string trimmedContent = content == null ? string.Empty : content.Trim();
        if (trimmedContent.Length == 0)
        {
            return ChatOperationResult<ChatMessage>.CreateFailure(
                ChatOperationStatus.EmptyMessage);
        }
        if (trimmedContent.Length > 200)
        {
            return ChatOperationResult<ChatMessage>.CreateFailure(
                ChatOperationStatus.MessageTooLong);
        }

        ChatOperationResult<ChatMessage> result;
        try
        {
            long accountId = playerResult.GetValue();
            ChatMessage message = _chat.Send(new ChatMessageSubmission
            {
                AccountId = accountId,
                MatchId = matchId,
                Content = trimmedContent,
                NowUtc = _clock.UtcNow
            });
            result = ChatOperationResult<ChatMessage>.CreateSuccess(message);
        }
        catch (MatchRejectedException exception)
        {
            result = ChatOperationResult<ChatMessage>.CreateFailure(
                GetStatus(exception.Reason));
        }
        catch (ChatRejectedException exception)
        {
            result = ChatOperationResult<ChatMessage>.CreateFailure(
                GetStatus(exception.Reason),
                exception.Mute);
        }

        return result;
    }

    /// <summary>Loads the authorized durable conversation in message order.</summary>
    /// <param name="sessionToken">The authenticated session token.</param>
    /// <param name="matchId">The durable match identifier.</param>
    /// <returns>The expected history operation result.</returns>
    public ChatOperationResult<ChatMessage[]> GetHistory(
        string sessionToken,
        long matchId)
    {
        ChatOperationResult<long> playerResult = GetPlayerAccountId(sessionToken);
        if (playerResult.Status != ChatOperationStatus.Success)
        {
            return ChatOperationResult<ChatMessage[]>.CreateFailure(playerResult.Status);
        }
        if (matchId <= 0)
        {
            return ChatOperationResult<ChatMessage[]>.CreateFailure(
                ChatOperationStatus.InvalidMatchId);
        }

        ChatOperationResult<ChatMessage[]> result;
        try
        {
            long accountId = playerResult.GetValue();
            result = ChatOperationResult<ChatMessage[]>.CreateSuccess(
                _chat.History(accountId, matchId));
        }
        catch (MatchRejectedException exception)
        {
            result = ChatOperationResult<ChatMessage[]>.CreateFailure(
                GetStatus(exception.Reason));
        }

        return result;
    }

    private ChatOperationResult<long> GetPlayerAccountId(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ChatOperationResult<long>.CreateFailure(
                ChatOperationStatus.MissingSessionToken);
        }
        if (!_sessions.TryGet(token, out AuthenticatedSession? player)
            || player == null
            || player.ExpiresAtUtc <= _clock.UtcNow)
        {
            return ChatOperationResult<long>.CreateFailure(
                ChatOperationStatus.InvalidSession);
        }

        return ChatOperationResult<long>.CreateSuccess(player.AccountId);
    }

    private static ChatOperationStatus GetStatus(MatchRejection rejection)
    {
        ChatOperationStatus status = ChatOperationStatus.Unauthorized;
        if (rejection == MatchRejection.InvalidMatchId)
        {
            status = ChatOperationStatus.InvalidMatchId;
        }
        else if (rejection == MatchRejection.MatchNotFound)
        {
            status = ChatOperationStatus.MatchNotFound;
        }
        else if (rejection == MatchRejection.MatchNotInProgress)
        {
            status = ChatOperationStatus.MatchNotInProgress;
        }
        else if (rejection == MatchRejection.InactiveAccount)
        {
            status = ChatOperationStatus.InactiveAccount;
        }

        return status;
    }

    private static ChatOperationStatus GetStatus(ChatRejection rejection)
    {
        ChatOperationStatus status = ChatOperationStatus.MessageBlocked;
        if (rejection == ChatRejection.EmptyMessage)
        {
            status = ChatOperationStatus.EmptyMessage;
        }
        else if (rejection == ChatRejection.MessageTooLong)
        {
            status = ChatOperationStatus.MessageTooLong;
        }
        else if (rejection == ChatRejection.PlayerMuted)
        {
            status = ChatOperationStatus.PlayerMuted;
        }

        return status;
    }
}
