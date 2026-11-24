using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Persistence.Verification;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Guards against successful builds that omit the runtime persistence model.</summary>
[TestClass]
public sealed class ModelResourcesTests
{
    [TestMethod]
    [DataRow("Model.Database_model.csdl")]
    [DataRow("Model.Database_model.ssdl")]
    [DataRow("Model.Database_model.msl")]
    public void Assembly_RequiredRuntimeModel_IsEmbedded(string resourceName)
    {
        bool expectedEmbedded = true;

        string[] resources = typeof(VerificationCodeRepository).Assembly.GetManifestResourceNames();
        bool actualEmbedded = resources.Contains(resourceName);

        Assert.AreEqual(
            expectedEmbedded,
            actualEmbedded,
            "The persistence model must be available in the compiled assembly: " + resourceName);
    }
}
