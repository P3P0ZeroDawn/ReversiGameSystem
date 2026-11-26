using System;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests.TestDoubles;

internal sealed class ChatRepositoryStub : IChatRepository
{
    internal ChatRepositoryStub()
    {
        Submission = new ChatMessageSubmission(new ChatMessage());
    }

    internal int AccessEvaluationCalls { get; private set; }

    internal int LoadChannelAccessCalls { get; private set; }

    internal int SubmissionCalls { get; private set; }

    internal int LoadHistoryCalls { get; private set; }

    internal long LastAccountId { get; private set; }

    internal long LastMatchId { get; private set; }

    internal DateTime LastNowUtc { get; private set; }

    internal string LastContent { get; private set; } = string.Empty;

    internal int LastMaximumMessages { get; private set; }

    internal TimeSpan LastRateWindow { get; private set; }

    internal bool ThrowOnAccessEvaluation { get; set; }

    internal bool ThrowOnLoadChannelAccess { get; set; }

    internal bool ThrowOnSubmission { get; set; }

    internal bool ThrowOnLoadHistory { get; set; }

    internal ChatStatus AccessEvaluationStatus { get; set; } = ChatStatus.Success;

    internal ChatChannelAccess ChannelAccess { get; set; } = new ChatChannelAccess(null);

    internal ChatMessageSubmission Submission { get; set; }

    internal ChatHistory History { get; set; } = new ChatHistory(Array.Empty<ChatMessage>());

    public ChatStatus EvaluateParticipantAccess(long accountId, long matchId)
    {
        AccessEvaluationCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        if (ThrowOnAccessEvaluation)
        {
            throw new InvalidOperationException("Simulated chat repository failure.");
        }

        return AccessEvaluationStatus;
    }

    public ChatChannelAccess LoadChannelAccess(long accountId, long matchId, DateTime nowUtc)
    {
        LoadChannelAccessCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        LastNowUtc = nowUtc;
        if (ThrowOnLoadChannelAccess)
        {
            throw new InvalidOperationException("Simulated chat repository failure.");
        }

        return ChannelAccess;
    }

    public ChatMessageSubmission ProcessMessageSubmission(
        long accountId,
        long matchId,
        string content,
        int maximumMessages,
        TimeSpan rateWindow)
    {
        SubmissionCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        LastContent = content;
        LastMaximumMessages = maximumMessages;
        LastRateWindow = rateWindow;
        if (ThrowOnSubmission)
        {
            throw new InvalidOperationException("Simulated chat repository failure.");
        }

        return Submission;
    }

    public ChatHistory LoadMessageHistory(long accountId, long matchId)
    {
        LoadHistoryCalls++;
        LastAccountId = accountId;
        LastMatchId = matchId;
        if (ThrowOnLoadHistory)
        {
            throw new InvalidOperationException("Simulated chat repository failure.");
        }

        return History;
    }
}
