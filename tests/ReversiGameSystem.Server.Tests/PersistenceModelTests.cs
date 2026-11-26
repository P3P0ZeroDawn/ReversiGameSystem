using System;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class PersistenceModelTests
{
    [TestMethod]
    public void CreateContext_ValidEnvironment_LoadsEmbeddedModel()
    {
        const string variableName = "REVERSI_DB_CONNECTION_STRING";
        string? originalValue = Environment.GetEnvironmentVariable(variableName);
        int expectedEntityCount = 13;
        try
        {
            Environment.SetEnvironmentVariable(
                variableName,
                "Server=localhost;Database=ReversiGameSystem;" +
                "Integrated Security=True;TrustServerCertificate=True;");

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
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
