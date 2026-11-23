using System;

namespace ReversiGameSystem.Server.Application.Chat;

public interface IChatRepository
{
    public ChatStatus EvaluateParticipantAccess(long accountId, long matchId);

    public ChatChannelAccess LoadChannelAccess(
        long accountId,
        long matchId,
        DateTime nowUtc);

    public ChatMessageSubmission ProcessMessageSubmission(
        long accountId,
        long matchId,
        string content,
        int maximumMessages,
        TimeSpan rateWindow);

    public ChatHistory LoadMessageHistory(long accountId, long matchId);
}
