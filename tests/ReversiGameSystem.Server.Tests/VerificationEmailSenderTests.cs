using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests;

[TestClass]
public sealed class VerificationEmailSenderTests
{
    [TestMethod]
    public void Load_ConfiguredPort_ReturnsConfiguredPort()
    {
        int expectedPort = 587;

        int actualPort = 0;
        WithSettings(expectedPort, "StartTls", () =>
        {
            actualPort = MailSettings.Load().Port;
        });

        Assert.AreEqual(expectedPort, actualPort);
    }

    [TestMethod]
    [DataRow("StartTls", false)]
    [DataRow("Tls", true)]
    public void Load_MailSecurity_SetsEncryptOnConnect(
        string securityMode,
        bool expectedEncryptOnConnect)
    {
        bool actualEncryptOnConnect = !expectedEncryptOnConnect;
        WithSettings(587, securityMode, () =>
        {
            actualEncryptOnConnect = MailSettings.Load().EncryptOnConnect;
        });

        Assert.AreEqual(expectedEncryptOnConnect, actualEncryptOnConnect);
    }

    [TestMethod]
    [DataRow(587, "None")]
    [DataRow(65536, "StartTls")]
    public void Load_InvalidConfiguration_ThrowsInvalidOperationException(
        int port,
        string securityMode)
    {
        InvalidOperationException? actualException = null;

        WithSettings(port, securityMode, () =>
        {
            actualException = Assert.Throws<InvalidOperationException>(() => MailSettings.Load());
        });

        Assert.IsNotNull(actualException);
    }

    [TestMethod]
    public void Send_LocalServerWithheldBanner_FailsDelivery()
    {
        bool expectedFailed = true;

        (bool actualFailed, _, _) = ExecuteStalledSend();

        Assert.AreEqual(expectedFailed, actualFailed, "A stalled SMTP connection must fail delivery.");
    }

    [TestMethod]
    public void Send_LocalServerWithheldBanner_ConnectsToServer()
    {
        bool expectedConnected = true;

        (_, bool actualConnected, _) = ExecuteStalledSend();

        Assert.AreEqual(
            expectedConnected,
            actualConnected,
            "The local server must have accepted the connection.");
    }

    [TestMethod]
    public void Send_LocalServerWithheldBanner_CancelsWithinDeliveryBudget()
    {
        bool expectedWithinBudget = true;

        (_, _, TimeSpan elapsed) = ExecuteStalledSend();
        bool actualWithinBudget = elapsed >= TimeSpan.FromSeconds(4)
            && elapsed < TimeSpan.FromSeconds(7);

        Assert.AreEqual(
            expectedWithinBudget,
            actualWithinBudget,
            "The five-second deadline allows two seconds of scheduling tolerance.");
    }

    private static (bool Failed, bool Connected, TimeSpan Elapsed) ExecuteStalledSend()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task<TcpClient> connection = listener.AcceptTcpClientAsync();
            bool failed = false;
            Stopwatch elapsed = Stopwatch.StartNew();

            WithSettings(port, "StartTls", () =>
            {
                SmtpEmailSender sender = new SmtpEmailSender(MailSettings.Load());
                try
                {
                    sender.Send(new EmailMessage(
                        "recipient@example.invalid",
                        "Reversi: sign-in / inicio de sesión",
                        "Your verification code / Tu código de verificación: 000012"));
                }
                catch (Exception exception) when (exception is OperationCanceledException
                    || exception is System.IO.IOException || exception is TimeoutException
                    || exception is MailKit.ProtocolException)
                {
                    failed = true;
                }
                elapsed.Stop();
            });

            using (TcpClient client = connection.GetAwaiter().GetResult()) { }
            return (failed, connection.IsCompleted, elapsed.Elapsed);
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void WithSettings(int port, string securityMode, Action action)
    {
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            ["REVERSI_MAIL_HOST"] = "127.0.0.1",
            ["REVERSI_MAIL_PORT"] =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["REVERSI_MAIL_SECURITY"] = securityMode,
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
