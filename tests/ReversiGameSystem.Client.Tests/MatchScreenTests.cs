using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;
using ReversiGameSystem.Views;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Client.Tests;

/// <summary>Exercises the original game screen against the disposable real match host.</summary>
internal static class MatchScreenTests
{
    internal static void ExistingGamePage_RealMatch_RendersPlayersBoardAndChat()
    {
        Exception? failure = null;
        Thread ui = new Thread(() =>
        {
            try
            {
                RunScreen();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }) { IsBackground = true };
        ui.SetApartmentState(ApartmentState.STA);
        ui.Start();
        if (!ui.Join(TimeSpan.FromSeconds(55)))
        {
            throw new TimeoutException("The game screen integration did not finish.");
        }
        if (failure != null)
        {
            throw new InvalidOperationException("The original game screen integration failed.", failure);
        }
    }

    private static void RunScreen()
    {
        // Arrange: real generated consumers, synthetic accounts and a disposable database.
        App app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.InitializeComponent();
        SessionClient sessions = new SessionClient(NullLogger<SessionClient>.Instance,
            () => new Proxy.SessionServiceClient(new NetTcpBinding(SecurityMode.Transport),
                new EndpointAddress("net.tcp://localhost:18091/ReversiReview/Session")));
        Proxy.AuthenticatedSession alpha = sessions.CompleteLoginAsync("ReviewAlpha", "000001")
            .GetAwaiter().GetResult();
        Proxy.AuthenticatedSession beta = sessions.CompleteLoginAsync("ReviewBeta", "000002")
            .GetAwaiter().GetResult();
        using RemoteMatchClient first = RemoteMatchClientTests.Create(alpha.SessionToken);
        using RemoteMatchClient second = RemoteMatchClientTests.Create(beta.SessionToken);
        Window window = new Window
        {
            Width = 1280, Height = 850, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000, Top = -20000
        };
        try
        {
            Proxy.LobbySnapshot waiting = first.CreateAsync(40).GetAwaiter().GetResult();
            second.JoinAsync(waiting.MatchCode).GetAwaiter().GetResult();
            second.LoadStateAsync(waiting.MatchId).GetAwaiter().GetResult();
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            GamePage page = new GamePage(first, waiting.MatchId, null);
            window.Content = new Frame { Content = page, NavigationUIVisibility =
                System.Windows.Navigation.NavigationUIVisibility.Hidden };
            window.Show();
            Button send = Control<Button>(page, "sendMessageButton");
            ListBox messages = Control<ListBox>(page, "chatMessagesListBox");
            PumpUntil(() => send.IsEnabled);
            TestAssert.AreEqual("ReviewAlpha", Control<TextBlock>(page, "creatorNameTextBlock").Text);
            TestAssert.AreEqual("ReviewBeta", Control<TextBlock>(page, "opponentNameTextBlock").Text);
            PumpUntil(() => Control<TextBlock>(page, "creatorConnectionTextBlock").Text
                == LocalizationManager.Instance["Game_PresenceConnected"]
                && Control<TextBlock>(page, "opponentConnectionTextBlock").Text
                == LocalizationManager.Instance["Game_PresenceConnected"]);
            ReversiBoardControl board = Control<ReversiBoardControl>(page, "gameBoardControl");
            TestAssert.AreEqual(4, Control<Grid>(board, "piecesGrid").Children.Count);
            foreach (Image piece in Control<Grid>(board, "piecesGrid").Children)
            {
                TestAssert.AreEqual(true, ((BitmapSource)piece.Source).PixelWidth > 0);
            }
            TestAssert.AreEqual(false, Control<Grid>(board, "interactionGrid").IsHitTestVisible);

            // Act: send through the existing composer, then receive the other player's callback.
            Control<TextBox>(page, "chatMessageTextBox").Text = "Hello from the original screen";
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpUntil(() => messages.Items.Cast<object>().Any(item =>
                item.ToString().Contains("Hello from the original screen"))
                && Control<TextBox>(page, "chatMessageTextBox").Text.Length == 0);
            AwaitUi(second.OpenChatAsync(waiting.MatchId));
            AwaitUi(second.SendAsync(waiting.MatchId, "Reply from the second player"));
            PumpUntil(() => messages.Items.Count == 2);

            // Assert: preserve the same screen and render both supported languages.
            foreach (string culture in new[] { "en-US", "es-MX" })
            {
                LocalizationManager.Instance.CultureName = culture;
                page.UpdateLayout();
                PumpUntil(() => messages.Items.Count == 2);
                TestAssert.AreEqual(string.Format(LocalizationManager.Instance["Game_ScoreFormat"], 2),
                    Control<TextBlock>(page, "creatorScoreTextBlock").Text);
                TestAssert.AreEqual(true, messages.Items.Cast<object>().Any(item =>
                    item.ToString().Contains("Reply from the second player")));
                SavePreview(page, culture);
            }
            Console.WriteLine("Passed original GamePage rendering and two-way UI chat in en-US and es-MX.");
        }
        finally
        {
            window.Close();
            SynchronizationContext.SetSynchronizationContext(null);
            sessions.CloseSessionAsync(beta.SessionToken).GetAwaiter().GetResult();
            sessions.CloseSessionAsync(alpha.SessionToken).GetAwaiter().GetResult();
            app.Shutdown();
        }
    }

    private static T Control<T>(FrameworkElement owner, string name) where T : FrameworkElement =>
        (T)(owner.FindName(name) ?? throw new InvalidOperationException("Missing screen control: " + name));

    private static void AwaitUi(Task request)
    {
        PumpUntil(() => request.IsCompleted);
        request.GetAwaiter().GetResult();
    }

    private static void PumpUntil(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        do
        {
            DispatcherFrame frame = new DispatcherFrame();
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            Dispatcher.PushFrame(frame);
            if (condition())
            {
                return;
            }
        } while (DateTime.UtcNow < deadline);
        throw new TimeoutException("The existing screen did not receive its confirmed remote data.");
    }

    private static void SavePreview(FrameworkElement page, string culture)
    {
        RenderTargetBitmap bitmap = new RenderTargetBitmap((int)page.ActualWidth,
            (int)page.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(page);
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        string path = Path.Combine(Path.GetTempPath(), "ReversiGamePage-" + culture + ".png");
        using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
        Console.WriteLine("Screen preview: " + path);
    }
}
