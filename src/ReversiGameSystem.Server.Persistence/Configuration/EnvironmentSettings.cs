using System;
using System.Collections.Generic;
using System.IO;

namespace ReversiGameSystem.Server.Persistence.Configuration;

/// <summary>Reads server configuration without exposing its values in diagnostics.</summary>
public static class EnvironmentSettings
{
    /// <summary>Reads a required process variable or a local development file value.</summary>
    /// <param name="name">The configuration variable name.</param>
    /// <returns>The configured value.</returns>
    /// <exception cref="InvalidOperationException">The required value is absent.</exception>
    public static string ReadRequired(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (value != null && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        string? path = FindFile();
        if (path != null)
        {
            value = ReadFileValue(path, name);
        }

        if (value == null || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The {name} variable is required in the environment or local .env file.");
        }

        return value;
    }

    private static string? FindFile()
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return FindFrom(AppDomain.CurrentDomain.BaseDirectory, visited)
            ?? FindFrom(Environment.CurrentDirectory, visited);
    }

    private static string? FindFrom(string start, ISet<string> visited)
    {
        DirectoryInfo? directory = new DirectoryInfo(start);
        while (directory != null && visited.Add(directory.FullName))
        {
            string path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? ReadFileValue(string path, string name)
    {
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator > 0 && string.Equals(line.Substring(0, separator).Trim(),
                name, StringComparison.Ordinal))
            {
                return Unquote(line.Substring(separator + 1).Trim());
            }
        }

        return null;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"')
            || (value[0] == '\'' && value[value.Length - 1] == '\'')))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
}
