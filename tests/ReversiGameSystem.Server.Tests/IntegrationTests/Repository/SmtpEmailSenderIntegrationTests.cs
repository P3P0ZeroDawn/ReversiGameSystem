using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MailKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Server.Application.Common;
using ReversiGameSystem.Server.Persistence.Mail;

namespace ReversiGameSystem.Server.Tests.IntegrationTests.Repository;

[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public sealed class SmtpEmailSenderIntegrationTests
{
    private const string StartTlsSecurity = "StartTls";

    [TestMethod]
    public void Load_ConfiguredPort_ReturnsConfiguredPort()
    {
        int expectedPort = 587;
        int actualPort = 0;

        WithSettings(expectedPort, StartTlsSecurity, () =>
        {
            actualPort = MailSettings.Load().Port;
        });

        Assert.AreEqual(expectedPort, actualPort);
    }

    [TestMethod]
    [DataRow(StartTlsSecurity, false)]
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
    [DataRow(65536, StartTlsSecurity)]
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
    public void Send_LocalServerWithheldBanner_FailsConnectsAndCancelsWithinBudget()
    {
        bool expectedFailed = true;
        bool expectedConnected = true;
        bool expectedWithinBudget = true;

        (bool actualFailed, bool actualConnected, TimeSpan elapsed) = ExecuteStalledSend();
        bool actualWithinBudget = elapsed >= TimeSpan.FromSeconds(4)
            && elapsed < TimeSpan.FromSeconds(10);

        Assert.AreEqual(expectedFailed, actualFailed);
        Assert.AreEqual(expectedConnected, actualConnected);
        Assert.AreEqual(expectedWithinBudget, actualWithinBudget);
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

            WithSettings(port, StartTlsSecurity, () =>
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
                    || exception is IOException
                    || exception is TimeoutException
                    || exception is ProtocolException)
                {
                    failed = true;
                }

                elapsed.Stop();
            });

            bool connected = connection.Wait(TimeSpan.FromSeconds(1));
            if (connected)
            {
                using TcpClient client = connection.GetAwaiter().GetResult();
            }

            return (failed, connected, elapsed.Elapsed);
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
            ["REVERSI_MAIL_PORT"] = port.ToString(CultureInfo.InvariantCulture),
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
