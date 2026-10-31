using System;
using System.Collections.Generic;
using System.IO;

namespace ReversiGameSystem.Configuration;

/// <summary>
/// Reads client runtime environment configuration from process variables or local .env file.
/// </summary>
internal static class ClientEnvironmentSettings
{
    /// <summary>
    /// Reads an optional process variable or local .env file value, falling back to a default.
    /// </summary>
    /// <param name="name">The configuration variable name.</param>
    /// <param name="defaultValue">The fallback value if absent or whitespace.</param>
    /// <returns>The resolved configuration value or default value.</returns>
    internal static string ReadOptional(string name, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Variable name cannot be null or whitespace.",
                nameof(name));
        }

        string? value = Environment.GetEnvironmentVariable(name);
        if (value != null)
        {
            return value;
        }

        string? path = FindFile();
        if (path != null)
        {
            value = ReadFileValue(path, name);
            if (value != null)
            {
                return value;
            }
        }

        return defaultValue;
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
            if (separator > 0 && string.Equals(
                line.Substring(0, separator).Trim(),
                name,
                StringComparison.Ordinal))
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
