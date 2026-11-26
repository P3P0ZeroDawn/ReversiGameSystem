using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Preferences;

namespace ReversiGameSystem.Server.Tests.UnitTests.Application.PreferencesModelsTests;

[TestClass]
[TestCategory("Unit")]
public sealed class PreferencesAccessTests
{
    [TestMethod]
    public void Constructor_SuccessStatusWithoutPreferences_ThrowsArgumentOutOfRangeException()
    {
        const string expectedParamName = "status";
        Action action = () => _ = new PreferencesAccess(PreferencesStatus.Success);

        ArgumentOutOfRangeException exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void Constructor_NullPreferences_ThrowsArgumentNullException()
    {
        const string expectedParamName = "preferences";
        Action action = () => _ = new PreferencesAccess(null!);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        string actualParamName = exception.ParamName;

        Assert.AreEqual(expectedParamName, actualParamName);
    }

    [TestMethod]
    public void BackgroundImage_RejectedAccess_ThrowsInvalidOperationException()
    {
        PreferencesAccess access = new PreferencesAccess(PreferencesStatus.InvalidSession);
        const string expectedMessage = "The preferences access was rejected.";
        Action action = () => _ = access.BackgroundImage;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(action);
        string actualMessage = exception.Message;

        Assert.AreEqual(expectedMessage, actualMessage);
    }

    [TestMethod]
    public void Preferences_RejectedAccess_ThrowsInvalidOperationException()
    {
        PreferencesAccess access = new PreferencesAccess(PreferencesStatus.InvalidSession);
        const string expectedMessage = "The preferences access was rejected.";
        Action action = () => _ = access.Preferences;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(action);
        string actualMessage = exception.Message;

        Assert.AreEqual(expectedMessage, actualMessage);
    }
}
