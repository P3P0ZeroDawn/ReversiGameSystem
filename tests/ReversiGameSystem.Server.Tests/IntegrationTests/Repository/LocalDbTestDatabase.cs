using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

internal sealed class LocalDbTestDatabase : IDisposable
{
    private const string LocalDbServer = @"(localdb)\MSSQLLocalDB";
    private const string MasterDatabaseName = "master";
    private const string ConnectionVariable = "REVERSI_DB_CONNECTION_STRING";
    private const string SchemaDirectoryName = "Fixtures";
    private const string SchemaFileName = "RepositoryIntegrationSchema.sql";
    private const string DatabaseNamePrefix = "ReversiRepoTests_";
    private const int BatchRegexTimeoutSeconds = 1;
    private static readonly TimeSpan BatchRegexTimeout =
        TimeSpan.FromSeconds(BatchRegexTimeoutSeconds);

    private readonly string _databaseName;
    private readonly string _previousEnvironmentConnection;
    private bool _disposed;

    internal LocalDbTestDatabase()
    {
        _databaseName = DatabaseNamePrefix + Guid.NewGuid().ToString("N");
        _previousEnvironmentConnection =
            Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;

        ConnectionString = BuildConnectionString(_databaseName);
        try
        {
            CreateDatabase();
            ApplySchema();
            Environment.SetEnvironmentVariable(ConnectionVariable, ConnectionString);
        }
        catch
        {
            DropDatabase();
            throw;
        }
    }

    internal string ConnectionString { get; }

    internal void ResetMutableTables()
    {
        string resetSql =
            "UPDATE dbo.GameMatch SET currentTurnSeatNumber = NULL "
            + "WHERE currentTurnSeatNumber IS NOT NULL; "
            + "DELETE FROM dbo.ChatMute; "
            + "DELETE FROM dbo.ChatViolation; "
            + "DELETE FROM dbo.ChatMessage; "
            + "DELETE FROM dbo.GameMove; "
            + "DELETE FROM dbo.MatchParticipant; "
            + "DELETE FROM dbo.GameMatch; "
            + "DELETE FROM dbo.VerificationCode; "
            + "DELETE FROM dbo.Friendship; "
            + "DELETE FROM dbo.AccountSettings; "
            + "DELETE FROM dbo.Account;";

        using SqlConnection connection = new SqlConnection(ConnectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand(resetSql, connection);
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        string? restoredValue = string.IsNullOrEmpty(_previousEnvironmentConnection)
            ? null
            : _previousEnvironmentConnection;
        Environment.SetEnvironmentVariable(ConnectionVariable, restoredValue);

        DropDatabase();
    }

    private static string BuildConnectionString(string databaseName)
    {
        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
        {
            DataSource = LocalDbServer,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            MultipleActiveResultSets = true
        };
        return builder.ConnectionString;
    }

    private void CreateDatabase()
    {
        string masterConnectionString = BuildConnectionString(MasterDatabaseName);
        using SqlConnection connection = new SqlConnection(masterConnectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand(
            $"CREATE DATABASE [{_databaseName}];",
            connection);
        command.ExecuteNonQuery();
    }

    private void ApplySchema()
    {
        string schemaPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            SchemaDirectoryName,
            SchemaFileName);
        string script = File.ReadAllText(schemaPath);
        string[] batches = Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase,
            BatchRegexTimeout);

        using SqlConnection connection = new SqlConnection(ConnectionString);
        connection.Open();
        foreach (string batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            using SqlCommand command = new SqlCommand(batch, connection);
            command.ExecuteNonQuery();
        }
    }

    private void DropDatabase()
    {
        SqlConnection.ClearAllPools();
        string masterConnectionString = BuildConnectionString(MasterDatabaseName);
        using SqlConnection connection = new SqlConnection(masterConnectionString);
        connection.Open();
        string dropSql =
            $"IF DB_ID(N'{_databaseName}') IS NOT NULL "
            + "BEGIN "
            + $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; "
            + $"DROP DATABASE [{_databaseName}]; "
            + "END";
        using SqlCommand command = new SqlCommand(dropSql, connection);
        command.ExecuteNonQuery();
    }
}
