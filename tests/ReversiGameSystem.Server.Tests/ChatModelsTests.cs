using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Chat;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class ChatModelsTests
{
    [TestMethod]
    public void ChatChannelAccess_SuccessStatus_RejectsStatusConstructor()
    {
        ChatStatus status = ChatStatus.Success;
        string expectedParamName = nameof(status);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChatChannelAccess(status));
        string? actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    [DataRow(ChatStatus.Success)]
    [DataRow(ChatStatus.PlayerMuted)]
    public void ChatMessageSubmission_StatusRequiringPayload_RejectsStatusConstructor(
        ChatStatus status)
    {
        string expectedParamName = nameof(status);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChatMessageSubmission(status));
        string? actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void ChatHistory_SuccessStatus_RejectsMissingMessages()
    {
        ChatStatus status = ChatStatus.Success;
        string expectedParamName = nameof(status);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChatHistory(status));
        string? actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }
}
