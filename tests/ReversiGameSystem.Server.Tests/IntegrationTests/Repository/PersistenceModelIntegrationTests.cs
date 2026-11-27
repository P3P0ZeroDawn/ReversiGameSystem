using System;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[TestCategory("Integration")]
public sealed class PersistenceModelIntegrationTests
{
    private const string ConnectionVariable = "REVERSI_DB_CONNECTION_STRING";

    [TestMethod]
    public void CreateContext_ValidEnvironment_LoadsEmbeddedModel()
    {
        string? originalValue = Environment.GetEnvironmentVariable(ConnectionVariable);
        int expectedEntityCount = 13;
        try
        {
            Environment.SetEnvironmentVariable(
                ConnectionVariable,
                "Server=localhost;Database=ReversiGameSystem;"
                + "Integrated Security=True;TrustServerCertificate=True;");

            using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
            ReadOnlyCollection<EntityType> entityTypes =
                ((IObjectContextAdapter)context)
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace);
            int actualEntityCount = entityTypes.Count;

            Assert.AreEqual(expectedEntityCount, actualEntityCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConnectionVariable, originalValue);
        }
    }

    [TestMethod]
    [DataRow("Model.Database_model.csdl")]
    [DataRow("Model.Database_model.ssdl")]
    [DataRow("Model.Database_model.msl")]
    public void Assembly_RequiredRuntimeModel_IsEmbedded(string resourceName)
    {
        bool expectedEmbedded = true;

        string[] resources =
            typeof(ReversiGameSystemEntities).Assembly.GetManifestResourceNames();
        bool actualEmbedded = resources.Contains(resourceName);

        Assert.AreEqual(expectedEmbedded, actualEmbedded);
    }
}
