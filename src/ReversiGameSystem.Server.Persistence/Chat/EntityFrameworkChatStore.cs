using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using ReversiGameSystem.Domain.Chat;
using ReversiGameSystem.Server.Application.Chat;
using ReversiGameSystem.Server.Application.Matches;
using ReversiGameSystem.Server.Persistence.Matches;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Persistence.Chat;

/// <summary>Persists messages and moderation before returning a confirmed result.</summary>
public sealed class EntityFrameworkChatStore : IChatStore
{
    /// <summary>Requires an active registered participant in an in-progress match.</summary>
    public void Authorize(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        RequireChatParticipant(context, accountId, matchId);
    }

    /// <summary>Tries to retrieve the account sanction currently in effect.</summary>
    public bool TryGetMute(long accountId, DateTime now, out ChatMuteRecord? mute)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        MatchDatabase.RequireActive(context, accountId);
        return TryLoadMute(context, new AccountModerationWindow(accountId, now), out mute);
    }

    /// <summary>Loads the authorized conversation in ascending durable message order.</summary>
    public ChatEntryRecord[] History(long accountId, long matchId)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        RequireChatParticipant(context, accountId, matchId);
        ChatEntryRecord[] result = context.ChatMessage.Where(message => message.matchId == matchId)
            .OrderBy(message => message.messageNumber).ToArray().Select(Map).ToArray();
        transaction.Commit();
        return result;
    }

    /// <summary>Commits either the accepted message or its moderation outcome before returning.</summary>
    public ChatEntryRecord Send(ChatSubmission submission)
    {
        using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
        using DbContextTransaction transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        MatchDatabase.Lock(context, "chat-account:" + submission.AccountId);
        MatchDatabase.Lock(context, "chat-match:" + submission.MatchId);
        MatchParticipant participant = RequireChatParticipant(context, submission.AccountId, submission.MatchId);
        AccountModerationWindow window = new AccountModerationWindow(submission.AccountId, submission.Now);
        if (TryLoadMute(context, window, out ChatMuteRecord? mute))
        {
            throw new ChatRejectedException(ChatRejection.PlayerMuted, mute);
        }
        if (TryFindPattern(context, submission.Content, out ModerationPattern? pattern))
        {
            ModerationDecision decision = RecordViolation(context, submission, pattern!);
            transaction.Commit();
            throw new ChatRejectedException(decision.Reason, decision.Mute);
        }
        ChatMessage message = new ChatMessage
        {
            matchId = submission.MatchId,
            messageNumber = (context.ChatMessage.Where(entry => entry.matchId == submission.MatchId)
                .Select(entry => (int?)entry.messageNumber).Max() ?? 0) + 1,
            senderSeatNumber = participant.seatNumber,
            content = submission.Content, sentAtUtc = submission.Now
        };
        context.ChatMessage.Add(message);
        context.SaveChanges();
        ChatEntryRecord result = Map(message);
        transaction.Commit();
        return result;
    }

    private static MatchParticipant RequireChatParticipant(
        ReversiGameSystemEntities context, long accountId, long matchId)
    {
        MatchParticipant participant = MatchDatabase.RequireParticipant(context, accountId, matchId);
        if (MatchDatabase.RequireMatch(context, matchId).statusCode != "IN_PROGRESS")
        {
            throw new MatchRejectedException(MatchRejection.MatchNotInProgress);
        }
        return participant;
    }

    private static bool TryLoadMute(
        ReversiGameSystemEntities context, AccountModerationWindow window, out ChatMuteRecord? result)
    {
        ChatMute? mute = context.ChatMute.Where(sanction =>
            sanction.ChatViolation.MatchParticipant.accountId == window.AccountId && sanction.endsAtUtc > window.Now)
            .OrderByDescending(sanction => sanction.endsAtUtc).FirstOrDefault();
        result = mute == null ? null : new ChatMuteRecord
        {
            StartsAtUtc = DateTime.SpecifyKind(mute.startsAtUtc, DateTimeKind.Utc),
            EndsAtUtc = DateTime.SpecifyKind(mute.endsAtUtc, DateTimeKind.Utc)
        };
        return result != null;
    }

    private static bool TryFindPattern(
        ReversiGameSystemEntities context, string content, out ModerationPattern? result)
    {
        result = context.ModerationPattern.Where(pattern => pattern.isEnabled && pattern.ModerationRule.isEnabled)
            .OrderBy(pattern => pattern.moderationPatternId).ToArray()
            .FirstOrDefault(pattern => ChatContentPolicy.Matches(content, pattern.patternValue));
        return result != null;
    }

    private static ModerationDecision RecordViolation(
        ReversiGameSystemEntities context, ChatSubmission submission, ModerationPattern pattern)
    {
        DateTime windowStart = submission.Now.AddHours(-24);
        int previous = context.ChatViolation.Count(violation =>
            violation.MatchParticipant.accountId == submission.AccountId
            && violation.detectedAtUtc >= windowStart && violation.detectedAtUtc <= submission.Now);
        ChatViolation violation = new ChatViolation
        {
            detectionEventId = Guid.NewGuid(), matchId = submission.MatchId,
            offenderSeatNumber = context.MatchParticipant.Where(player => player.matchId == submission.MatchId
                && player.accountId == submission.AccountId).Select(player => player.seatNumber).Single(),
            ruleCode = pattern.ruleCode, detectedLocaleCode = pattern.localeCode,
            detectedAtUtc = submission.Now
        };
        context.ChatViolation.Add(violation);
        ModerationDecision decision = new ModerationDecision();
        if (previous >= 2)
        {
            decision.Reason = ChatRejection.PlayerMuted;
            decision.Mute = new ChatMuteRecord
            {
                StartsAtUtc = submission.Now, EndsAtUtc = submission.Now.AddMinutes(10)
            };
            context.ChatMute.Add(new ChatMute
            {
                ChatViolation = violation, reasonCode = "REPEATED_VIOLATIONS",
                startsAtUtc = decision.Mute.StartsAtUtc, endsAtUtc = decision.Mute.EndsAtUtc
            });
        }
        context.SaveChanges();
        return decision;
    }

    private static ChatEntryRecord Map(ChatMessage message)
    {
        return new ChatEntryRecord
        {
            MatchId = message.matchId, MessageNumber = message.messageNumber,
            SenderSeatNumber = message.senderSeatNumber, Content = message.content,
            SentAtUtc = DateTime.SpecifyKind(message.sentAtUtc, DateTimeKind.Utc)
        };
    }

    private sealed class AccountModerationWindow
    {
        internal AccountModerationWindow(long accountId, DateTime now)
        {
            AccountId = accountId;
            Now = now;
        }

        internal long AccountId { get; }
        internal DateTime Now { get; }
    }

    private sealed class ModerationDecision
    {
        internal ChatRejection Reason { get; set; } = ChatRejection.MessageBlocked;
        internal ChatMuteRecord? Mute { get; set; }
    }
}
