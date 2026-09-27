using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Tests;

internal sealed class PersistenceModelTests
{
    internal void CreateContext_ValidEnvironment_LoadsEmbeddedModel()
    {
        const string variableName = "REVERSI_DB_CONNECTION_STRING";
        string? originalValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(
                variableName,
                "Server=localhost;Database=ReversiGameSystem;" +
                "Integrated Security=True;TrustServerCertificate=True;");

            using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
            var entityTypes = ((IObjectContextAdapter)context)
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace);

            TestAssert.AreEqual(13, entityTypes.Count);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
