using System.Linq;
using ReversiGameSystem.Server.Persistence.Verification;

namespace ReversiGameSystem.Server.Tests;

/// <summary>Guards against successful builds that omit the runtime persistence model.</summary>
internal static class ModelResourcesTests
{
    internal static void Assembly_RequiredRuntimeModel_IsEmbedded()
    {
        // Arrange
        string[] required =
        {
            "Model.Database_model.csdl",
            "Model.Database_model.ssdl",
            "Model.Database_model.msl"
        };

        // Act
        string[] resources = typeof(VerificationCodeRepository).Assembly.GetManifestResourceNames();

        // Assert
        foreach (string name in required)
        {
            TestAssert.IsTrue(resources.Contains(name),
                "The persistence model must be available in the compiled assembly: " + name);
        }
    }
}
