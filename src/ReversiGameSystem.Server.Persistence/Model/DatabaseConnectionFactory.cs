using System;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using ReversiGameSystem.Server.Persistence.Configuration;

namespace ReversiGameSystem.Server.Persistence.Model;

/// <summary>Creates the EF connection from server configuration outside source control.</summary>
internal static class DatabaseConnectionFactory
{
    internal const string ConnectionStringVariable = "REVERSI_DB_CONNECTION_STRING";
    private const string ModelMetadata =
        "res://*/Model.Database_model.csdl|"
        + "res://*/Model.Database_model.ssdl|res://*/Model.Database_model.msl";

    internal static string CreateEntityConnectionString()
    {
        string providerConnectionString = EnvironmentSettings.ReadRequired(ConnectionStringVariable);
        SqlConnectionStringBuilder sqlBuilder = ParseConnection(providerConnectionString);
        if (string.IsNullOrWhiteSpace(sqlBuilder.DataSource)
            || string.IsNullOrWhiteSpace(sqlBuilder.InitialCatalog))
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} variable must define Server and Database.");
        }

        EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder
        {
            Metadata = ModelMetadata,
            Provider = "System.Data.SqlClient",
            ProviderConnectionString = sqlBuilder.ConnectionString
        };
        return entityBuilder.ConnectionString;
    }

    private static SqlConnectionStringBuilder ParseConnection(string value)
    {
        try
        {
            return new SqlConnectionStringBuilder(value);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} variable does not contain "
                    + "a valid SQL Server connection string.", exception);
        }
    }
}
