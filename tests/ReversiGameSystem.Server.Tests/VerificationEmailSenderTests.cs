using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Verification;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class VerificationEmailSenderTests
{
    [TestMethod]
    public void Load_ProcessConfiguration_ValidatesPortAndRequiresEncryption()
    {
        // Arrange
        WithSettings(587, () =>
        {
            // Act
            MailSettings settings = MailSettings.Load();
            // Assert
            TestAssert.AreEqual(587, settings.Port);
            TestAssert.AreEqual(false, settings.EncryptOnConnect);
            Environment.SetEnvironmentVariable("REVERSI_MAIL_SECURITY", "None");
            ExpectInvalidConfiguration();
            Environment.SetEnvironmentVariable("REVERSI_MAIL_SECURITY", "Tls");
            TestAssert.AreEqual(true, MailSettings.Load().EncryptOnConnect);
            Environment.SetEnvironmentVariable("REVERSI_MAIL_PORT", "65536");
            ExpectInvalidConfiguration();
        });
    }

    [TestMethod]
    public void Send_LocalServerWithheldBanner_CancelsWithinDeliveryBudget()
    {
        // Arrange
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> connection = listener.AcceptTcpClientAsync();
            WithSettings(port, () =>
            {
                VerificationEmailSender sender = new VerificationEmailSender(MailSettings.Load());
                Stopwatch elapsed = Stopwatch.StartNew();
                bool failed = false;
                // Act
                try
                {
                    sender.Send(new VerificationEmail
                    {
                        RecipientAddress = "recipient@example.invalid", Code = "000012",
                        Purpose = VerificationPurpose.Login,
                        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
                    });
                }
                catch (Exception exception) when (exception is OperationCanceledException
                    || exception is System.IO.IOException || exception is TimeoutException
                    || exception is MailKit.ProtocolException)
                {
                    failed = true;
                }
                elapsed.Stop();
                // Assert
                TestAssert.IsTrue(failed, "A stalled SMTP connection must fail delivery.");
                TestAssert.IsTrue(connection.IsCompleted,
                    "The local server must have accepted the connection.");
                TestAssert.IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(4)
                    && elapsed.Elapsed < TimeSpan.FromSeconds(7),
                    "The five-second deadline allows two seconds of scheduling tolerance.");
            });
            using (TcpClient client = connection.GetAwaiter().GetResult()) { }
        }
        finally { listener.Stop(); }
    }

    private static void ExpectInvalidConfiguration()
    {
        try { MailSettings.Load(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Expected rejected mail configuration.");
    }

    private static void WithSettings(int port, Action action)
    {
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["REVERSI_MAIL_HOST"] = "127.0.0.1",
            ["REVERSI_MAIL_PORT"] =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["REVERSI_MAIL_SECURITY"] = "StartTls",
            ["REVERSI_MAIL_USERNAME"] = "disposable-test-user",
            ["REVERSI_MAIL_PASSWORD"] = "disposable-test-password",
            ["REVERSI_MAIL_FROM_ADDRESS"] = "sender@example.invalid",
            ["REVERSI_MAIL_FROM_NAME"] = "Reversi Tests"
        };
        Dictionary<string, string?> previous = new Dictionary<string, string?>();
        try
        {
            foreach (KeyValuePair<string, string> entry in values)
            {
                previous.Add(entry.Key, Environment.GetEnvironmentVariable(entry.Key));
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
            action();
        }
        finally
        {
            foreach (KeyValuePair<string, string?> entry in previous)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }
        }
    }
}
