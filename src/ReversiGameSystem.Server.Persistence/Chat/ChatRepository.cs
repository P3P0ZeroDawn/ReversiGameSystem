using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Domain.Chat;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Model;
using ApplicationChatMessage = ReversiGameSystem.Server.Application.Chat.ChatMessage;
using ApplicationChatMute = ReversiGameSystem.Server.Application.Chat.ChatMute;
using PersistentChatMessage = ReversiGameSystem.Server.Persistence.Model.ChatMessage;
using PersistentChatMute = ReversiGameSystem.Server.Persistence.Model.ChatMute;

namespace ReversiGameSystem.Server.Persistence.Chat;

/// <summary>Persists authorized messages and moderation outcomes.</summary>
public class ChatRepository : IChatRepository
{
    private const string ActiveAccountStatusCode = "ACTIVE";
    private const string InProgressMatchStatusCode = "IN_PROGRESS";
    private const string AccountLockResourcePrefix = "chat-account:";
    private const string MatchLockResourcePrefix = "chat-match:";

    /// <inheritdoc />
    public ChatStatus EvaluateParticipantAccess(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        return EvaluateAccessStatus(context, accountId, matchId);
    }

    /// <inheritdoc />
    public ChatChannelAccess LoadChannelAccess(
        long accountId,
        long matchId,
        DateTime nowUtc)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        ChatStatus status = EvaluateAccessStatus(context, accountId, matchId);
        ChatChannelAccess result;
        if (status == ChatStatus.Success)
        {
            result = new ChatChannelAccess(LoadActiveMute(context, accountId, nowUtc));
        }
        else
        {
            result = new ChatChannelAccess(status);
        }

        return result;
    }

    /// <inheritdoc />
    public ChatHistory LoadMessageHistory(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        ChatStatus status = EvaluateAccessStatus(context, accountId, matchId);
        ChatHistory result;
        if (status == ChatStatus.Success)
        {
            ApplicationChatMessage[] messages = context.ChatMessage
                .Where(message => message.matchId == matchId)
                .OrderBy(message => message.messageNumber)
                .ToArray()
                .Select(MapChatMessage)
                .ToArray();
            transaction.Commit();
            result = new ChatHistory(messages);
        }
        else
        {
            result = new ChatHistory(status);
        }

        return result;
    }

    /// <inheritdoc />
    public ChatMessageSubmission ProcessMessageSubmission(
        long accountId,
        long matchId,
        string content,
        DateTime sentAtUtc)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(
            IsolationLevel.Serializable);
        MatchDatabase.AcquireTransactionLock(
            context,
            AccountLockResourcePrefix + accountId);
        MatchDatabase.AcquireTransactionLock(
            context,
            MatchLockResourcePrefix + matchId);

        ChatStatus accessStatus = EvaluateAccessStatus(context, accountId, matchId);
        if (accessStatus != ChatStatus.Success)
        {
            return new ChatMessageSubmission(accessStatus);
        }

        ApplicationChatMute? activeMute = LoadActiveMute(
            context,
            accountId,
            sentAtUtc);
        if (activeMute is not null)
        {
            return new ChatMessageSubmission(activeMute);
        }

        ModerationPattern? pattern = LoadModerationPattern(context, content);
        ChatMessageSubmission result;
        if (pattern is not null)
        {
            result = AddViolation(
                context,
                accountId,
                matchId,
                sentAtUtc,
                pattern);
        }
        else
        {
            result = AddMessage(
                context,
                accountId,
                matchId,
                content,
                sentAtUtc);
        }

        context.SaveChanges();
        transaction.Commit();
        return result;
    }

    private static ChatStatus EvaluateAccessStatus(
        ReversiGameSystemEntities context,
        long accountId,
        long matchId)
    {
        ChatStatus status = ChatStatus.Success;
        if (matchId <= 0)
        {
            status = ChatStatus.InvalidMatchId;
        }
        else if (!context.Account.Any(account =>
            account.accountId == accountId
            && account.statusCode == ActiveAccountStatusCode))
        {
            status = ChatStatus.InactiveAccount;
        }
        else
        {
            GameMatch? match = context.GameMatch.SingleOrDefault(candidate =>
                candidate.matchId == matchId);
            if (match is null)
            {
                status = ChatStatus.MatchNotFound;
            }
            else if (!context.MatchParticipant.Any(participant =>
                participant.matchId == matchId && participant.accountId == accountId))
            {
                status = ChatStatus.Unauthorized;
            }
            else if (match.statusCode != InProgressMatchStatusCode)
            {
                status = ChatStatus.MatchNotInProgress;
            }
        }

        return status;
    }

    private static ApplicationChatMute? LoadActiveMute(
        ReversiGameSystemEntities context,
        long accountId,
        DateTime nowUtc)
    {
        PersistentChatMute? mute = context.ChatMute
            .Where(sanction =>
                sanction.ChatViolation.MatchParticipant.accountId == accountId
                && sanction.endsAtUtc > nowUtc)
            .OrderByDescending(sanction => sanction.endsAtUtc)
            .FirstOrDefault();
        ApplicationChatMute? result = null;
        if (mute is not null)
        {
            result = new ApplicationChatMute
            {
                ReasonCode = mute.reasonCode,
                StartsAtUtc = DateTime.SpecifyKind(mute.startsAtUtc, DateTimeKind.Utc),
                EndsAtUtc = DateTime.SpecifyKind(mute.endsAtUtc, DateTimeKind.Utc)
            };
        }

        return result;
    }

    private static ModerationPattern? LoadModerationPattern(
        ReversiGameSystemEntities context,
        string content)
    {
        return context.ModerationPattern
            .Where(pattern => pattern.isEnabled && pattern.ModerationRule.isEnabled)
            .OrderBy(pattern => pattern.moderationPatternId)
            .ToArray()
            .FirstOrDefault(pattern => ChatContentPolicy.Matches(
                content,
                pattern.patternValue));
    }

    private static ChatMessageSubmission AddViolation(
        ReversiGameSystemEntities context,
        long accountId,
        long matchId,
        DateTime detectedAtUtc,
        ModerationPattern pattern)
    {
        DateTime windowStart = detectedAtUtc.Subtract(
            ChatContentPolicy.ViolationWindow);
        int previousViolations = context.ChatViolation.Count(violation =>
            violation.MatchParticipant.accountId == accountId
            && violation.detectedAtUtc >= windowStart
            && violation.detectedAtUtc <= detectedAtUtc);
        ChatViolation violation = new ChatViolation
        {
            detectionEventId = Guid.NewGuid(),
            matchId = matchId,
            offenderSeatNumber = context.MatchParticipant
                .Where(participant =>
                    participant.matchId == matchId && participant.accountId == accountId)
                .Select(participant => participant.seatNumber)
                .Single(),
            ruleCode = pattern.ruleCode,
            detectedLocaleCode = pattern.localeCode,
            detectedAtUtc = detectedAtUtc
        };
        context.ChatViolation.Add(violation);

        int currentViolationCount = previousViolations + 1;
        ChatMessageSubmission result;
        if (currentViolationCount >= ChatContentPolicy.ViolationsRequiredForMute)
        {
            ApplicationChatMute mute = new ApplicationChatMute
            {
                ReasonCode = ChatContentPolicy.RepeatedViolationsReasonCode,
                StartsAtUtc = detectedAtUtc,
                EndsAtUtc = detectedAtUtc.Add(ChatContentPolicy.MuteDuration)
            };
            context.ChatMute.Add(new PersistentChatMute
            {
                ChatViolation = violation,
                reasonCode = mute.ReasonCode,
                startsAtUtc = mute.StartsAtUtc,
                endsAtUtc = mute.EndsAtUtc
            });
            result = new ChatMessageSubmission(mute);
        }
        else
        {
            result = new ChatMessageSubmission(ChatStatus.MessageBlocked);
        }

        return result;
    }

    private static ChatMessageSubmission AddMessage(
        ReversiGameSystemEntities context,
        long accountId,
        long matchId,
        string content,
        DateTime sentAtUtc)
    {
        byte seatNumber = context.MatchParticipant
            .Where(participant =>
                participant.matchId == matchId && participant.accountId == accountId)
            .Select(participant => participant.seatNumber)
            .Single();
        PersistentChatMessage message = new PersistentChatMessage
        {
            matchId = matchId,
            messageNumber = (context.ChatMessage
                .Where(entry => entry.matchId == matchId)
                .Select(entry => (int?)entry.messageNumber)
                .Max() ?? 0) + 1,
            senderSeatNumber = seatNumber,
            content = content,
            sentAtUtc = sentAtUtc
        };
        context.ChatMessage.Add(message);
        return new ChatMessageSubmission(MapChatMessage(message));
    }

    private static ApplicationChatMessage MapChatMessage(PersistentChatMessage message)
    {
        return new ApplicationChatMessage
        {
            MatchId = message.matchId,
            MessageNumber = message.messageNumber,
            SenderSeatNumber = message.senderSeatNumber,
            Content = message.content,
            SentAtUtc = DateTime.SpecifyKind(message.sentAtUtc, DateTimeKind.Utc)
        };
    }
}
