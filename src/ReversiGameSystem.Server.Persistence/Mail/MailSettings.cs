using System;
using System.Globalization;
using MimeKit;
using ReversiGameSystem.Server.Persistence.Configuration;

namespace ReversiGameSystem.Server.Persistence.Mail;

/// <summary>Contains validated delivery configuration injected into the mail adapter.</summary>
public sealed class MailSettings
{
    private MailSettings() { }

    /// <summary>Gets the provider hostname.</summary>
    public string Host { get; private set; } = string.Empty;

    /// <summary>Gets the provider port.</summary>
    public int Port { get; private set; }

    /// <summary>Gets whether encryption begins immediately on connection.</summary>
    public bool EncryptOnConnect { get; private set; }

    /// <summary>Gets the provider account name.</summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>Gets the provider account credential.</summary>
    public string Password { get; private set; } = string.Empty;

    /// <summary>Gets the authorized sender address.</summary>
    public string FromAddress { get; private set; } = string.Empty;

    /// <summary>Gets the public sender name.</summary>
    public string FromName { get; private set; } = string.Empty;

    /// <summary>
    /// Loads and validates delivery configuration without reporting secret values.
    /// </summary>
    /// <returns>The validated server delivery settings.</returns>
    /// <exception cref="InvalidOperationException">
    /// Delivery configuration is absent or invalid.
    /// </exception>
    public static MailSettings Load()
    {
        MailSettings settings = new MailSettings
        {
            Host = EnvironmentSettings.ReadRequired("REVERSI_MAIL_HOST").Trim(),
            Username = EnvironmentSettings.ReadRequired("REVERSI_MAIL_USERNAME"),
            Password = EnvironmentSettings.ReadRequired("REVERSI_MAIL_PASSWORD"),
            FromAddress = EnvironmentSettings.ReadRequired("REVERSI_MAIL_FROM_ADDRESS").Trim(),
            FromName = EnvironmentSettings.ReadRequired("REVERSI_MAIL_FROM_NAME").Trim()
        };
        settings.Port = ReadPort();
        settings.EncryptOnConnect = ReadSecurity();
        if (settings.Host.IndexOfAny(new[] { '\r', '\n', '/', ' ' }) >= 0
            || !MailboxAddress.TryParse(settings.FromAddress, out MailboxAddress? sender)
            || sender.Address != settings.FromAddress)
        {
            throw new InvalidOperationException("The mail host or sender address is invalid.");
        }

        return settings;
    }

    private static int ReadPort()
    {
        string value = EnvironmentSettings.ReadRequired("REVERSI_MAIL_PORT");
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port)
            || port < 1 || port > 65535)
        {
            throw new InvalidOperationException("The REVERSI_MAIL_PORT variable is invalid.");
        }

        return port;
    }

    private static bool ReadSecurity()
    {
        string value = EnvironmentSettings.ReadRequired("REVERSI_MAIL_SECURITY");
        if (string.Equals(value, "StartTls", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(value, "Tls", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        throw new InvalidOperationException("REVERSI_MAIL_SECURITY must be StartTls or Tls.");
    }
}
