using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;

namespace ReversiGameSystem.Server.Persistence.Model;

/// <summary>
/// Creates the Entity Framework connection without storing machine-specific
/// database settings in source control.
/// </summary>
internal static class DatabaseConnectionFactory
{
    internal const string ConnectionStringVariable =
        "REVERSI_DB_CONNECTION_STRING";

    private const string ModelMetadata =
        "res://*/Model.Database_model.csdl|" +
        "res://*/Model.Database_model.ssdl|" +
        "res://*/Model.Database_model.msl";

    internal static string CreateEntityConnectionString()
    {
        string providerConnectionString = GetProviderConnectionString();

        SqlConnectionStringBuilder sqlBuilder;
        try
        {
            sqlBuilder = new SqlConnectionStringBuilder(providerConnectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} variable does not contain " +
                "a valid SQL Server connection string.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(sqlBuilder.DataSource)
            || string.IsNullOrWhiteSpace(sqlBuilder.InitialCatalog))
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringVariable} variable must define Server " +
                "and Database.");
        }

        EntityConnectionStringBuilder entityBuilder =
            new EntityConnectionStringBuilder
            {
                Metadata = ModelMetadata,
                Provider = "System.Data.SqlClient",
                ProviderConnectionString = sqlBuilder.ConnectionString
            };

        return entityBuilder.ConnectionString;
    }

    private static string GetProviderConnectionString()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(
            ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue;
        }

        string? environmentFile = FindEnvironmentFile();
        if (environmentFile != null
            && TryReadVariable(
                environmentFile,
                ConnectionStringVariable,
                out configuredValue))
        {
            return configuredValue!;
        }

        throw new InvalidOperationException(
            $"The {ConnectionStringVariable} variable was not found. Copy " +
            ".env.example to .env and configure your local SQL Server instance, " +
            "or define the system environment variable.");
    }

    private static string? FindEnvironmentFile()
    {
        HashSet<string> inspectedDirectories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? fromBaseDirectory = FindEnvironmentFileFrom(
            AppDomain.CurrentDomain.BaseDirectory,
            inspectedDirectories);
        if (fromBaseDirectory != null)
        {
            return fromBaseDirectory;
        }

        return FindEnvironmentFileFrom(
            Environment.CurrentDirectory,
            inspectedDirectories);
    }

    private static string? FindEnvironmentFileFrom(
        string startDirectory,
        ISet<string> inspectedDirectories)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory != null && inspectedDirectories.Add(directory.FullName))
        {
            string candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool TryReadVariable(
        string path,
        string variableName,
        out string? value)
    {
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0
                || !string.Equals(
                    line.Substring(0, separatorIndex).Trim(),
                    variableName,
                    StringComparison.Ordinal))
            {
                continue;
            }

            value = Unquote(line.Substring(separatorIndex + 1).Trim());
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[value.Length - 1] == '"')
                || (value[0] == '\'' && value[value.Length - 1] == '\'')))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
}
