using System;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests;

internal sealed class ChatModelsTests
{
    internal void ChatChannelAccess_SuccessStatus_RejectsStatusConstructor()
    {
        // Arrange
        ChatStatus status = ChatStatus.Success;

        // Act
        ArgumentOutOfRangeException exception =
            TestAssert.Throws<ArgumentOutOfRangeException>(
                () => new ChatChannelAccess(status));

        // Assert
        TestAssert.AreEqual(nameof(status), exception.ParamName);
    }

    internal void ChatMessageSubmission_SuccessStatus_RejectsMissingMessage()
    {
        // Arrange
        ChatStatus status = ChatStatus.Success;

        // Act
        ArgumentOutOfRangeException exception =
            TestAssert.Throws<ArgumentOutOfRangeException>(
                () => new ChatMessageSubmission(status));

        // Assert
        TestAssert.AreEqual(nameof(status), exception.ParamName);
    }

    internal void ChatMessageSubmission_PlayerMutedStatus_RejectsMissingMute()
    {
        // Arrange
        ChatStatus status = ChatStatus.PlayerMuted;

        // Act
        ArgumentOutOfRangeException exception =
            TestAssert.Throws<ArgumentOutOfRangeException>(
                () => new ChatMessageSubmission(status));

        // Assert
        TestAssert.AreEqual(nameof(status), exception.ParamName);
    }

    internal void ChatHistory_SuccessStatus_RejectsMissingMessages()
    {
        // Arrange
        ChatStatus status = ChatStatus.Success;

        // Act
        ArgumentOutOfRangeException exception =
            TestAssert.Throws<ArgumentOutOfRangeException>(
                () => new ChatHistory(status));

        // Assert
        TestAssert.AreEqual(nameof(status), exception.ParamName);
    }
}
