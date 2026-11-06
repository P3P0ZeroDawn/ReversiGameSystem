using System;

namespace ReversiGameSystem.Server.Application.Chat;

/// <summary>Defines authoritative chat access and atomic moderation operations.</summary>
public interface IChatRepository
{
    /// <summary>Requires an active registered participant in an active match.</summary>
    void Authorize(long accountId, long matchId);

    /// <summary>Tries to retrieve the account sanction currently in effect.</summary>
    bool TryGetMute(long accountId, DateTime nowUtc, out ChatMute? mute);

    /// <summary>Authorizes and commits the accepted message or moderation result atomically.</summary>
    ChatMessage Send(ChatMessageSubmission submission);

    /// <summary>Authorizes and loads the conversation in stable message order.</summary>
    ChatMessage[] History(long accountId, long matchId);
}
