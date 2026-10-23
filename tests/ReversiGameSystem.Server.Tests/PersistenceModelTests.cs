using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using ReversiGameSystem.Server.Persistence.Model;

namespace ReversiGameSystem.Server.Tests;

internal sealed class PersistenceModelTests
{
    internal void CreateContext_ValidEnvironment_LoadsEmbeddedModel()
    {
        // Arrange
        const string variableName = "REVERSI_DB_CONNECTION_STRING";
        string? originalValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(
                variableName,
                "Server=localhost;Database=ReversiGameSystem;" +
                "Integrated Security=True;TrustServerCertificate=True;");

            // Act
            using ReversiGameSystemEntities context = new ReversiGameSystemEntities();
            System.Collections.ObjectModel.ReadOnlyCollection<EntityType> entityTypes =
                ((IObjectContextAdapter)context)
                .ObjectContext
                .MetadataWorkspace
                .GetItems<EntityType>(DataSpace.CSpace);

            // Assert
            TestAssert.AreEqual(13, entityTypes.Count);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
