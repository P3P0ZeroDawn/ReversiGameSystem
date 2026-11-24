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
using ReversiGameSystem.Matches;
using ReversiGameSystem.Services;
using ReversiGameSystem.Views;
using Proxy = ReversiGameSystem.ServerProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[TestCategory("LiveIntegration")]
public sealed class MatchScreenTests
{
    [TestMethod]
    public void ExistingGamePage_RealMatch_RendersPlayersBoardAndChat()
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
        })
        { IsBackground = true };
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
            () => new Proxy.SessionServiceClient(new NetTcpBinding(SecurityMode.None),
                new EndpointAddress("net.tcp://localhost:18091/ReversiReview/Session")));
        AuthenticatedAccountSession alpha = Authenticate(sessions, "ReviewAlpha", "000001");
        AuthenticatedAccountSession beta = Authenticate(sessions, "ReviewBeta", "000002");
        using MatchLobbyClient firstLobby = MatchClientsTests.CreateLobby(alpha.SessionToken);
        using GameplayClient first = MatchClientsTests.CreateGameplay(alpha.SessionToken);
        using MatchChatClient firstChat = MatchClientsTests.CreateChat(alpha.SessionToken);
        using MatchLobbyClient secondLobby = MatchClientsTests.CreateLobby(beta.SessionToken);
        using GameplayClient second = MatchClientsTests.CreateGameplay(beta.SessionToken);
        using MatchChatClient secondChat = MatchClientsTests.CreateChat(beta.SessionToken);
        Window window = new Window
        {
            Width = 1280,
            Height = 850,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -20000,
            Top = -20000
        };
        try
        {
            MatchStatus creation = firstLobby.CreateMatch(40).GetAwaiter().GetResult();
            Assert.AreEqual(MatchStatus.Success, creation);
            LobbySnapshot waiting = firstLobby.ReadCurrentLobby();
            secondLobby.JoinMatch(waiting.MatchCode).GetAwaiter().GetResult();
            second.LoadMatchState(waiting.MatchId).GetAwaiter().GetResult();
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            GamePage page = new GamePage(first, firstChat, waiting.MatchId, null);
            window.Content = new Frame
            {
                Content = page,
                NavigationUIVisibility =
                System.Windows.Navigation.NavigationUIVisibility.Hidden
            };
            window.Show();
            Button send = Control<Button>(page, "sendMessageButton");
            ListBox messages = Control<ListBox>(page, "chatMessagesListBox");
            PumpUntil(() => send.IsEnabled);
            Assert.AreEqual("ReviewAlpha", Control<TextBlock>(page, "creatorNameTextBlock").Text);
            Assert.AreEqual("ReviewBeta", Control<TextBlock>(page, "opponentNameTextBlock").Text);
            PumpUntil(() => Control<TextBlock>(page, "creatorConnectionTextBlock").Text
                == LocalizationManager.Instance["Game_PresenceConnected"]
                && Control<TextBlock>(page, "opponentConnectionTextBlock").Text
                == LocalizationManager.Instance["Game_PresenceConnected"]);
            ReversiBoardControl board = Control<ReversiBoardControl>(page, "gameBoardControl");
            Assert.AreEqual(4, Control<Grid>(board, "piecesGrid").Children.Count);
            foreach (Image piece in Control<Grid>(board, "piecesGrid").Children)
            {
                Assert.AreEqual(true, ((BitmapSource)piece.Source).PixelWidth > 0);
            }
            Grid interaction = Control<Grid>(board, "interactionGrid");
            Assert.AreEqual(true, interaction.IsHitTestVisible);
            Assert.AreEqual(4, interaction.Children.Cast<Border>()
                .Count(square => square.Child != null));

            MatchStateSnapshot confirmedState = first.ReadMatchState(waiting.MatchId);
            Task<MatchStatus> placementRequest = first.PlacePiece(new MatchPlacement
            {
                MatchId = confirmedState.MatchId,
                ExpectedVersion = confirmedState.Version,
                Row = 2,
                Column = 3
            });
            PumpUntil(() => placementRequest.IsCompleted);
            MatchStatus placement = placementRequest.GetAwaiter().GetResult();
            Assert.AreEqual(MatchStatus.Success, placement);
            PumpUntil(() => Control<Grid>(board, "piecesGrid").Children.Count == 5);

            // Act: send through the existing composer, then receive the other player's callback.
            Control<TextBox>(page, "chatMessageTextBox").Text = "Hello from the original screen";
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpUntil(() => messages.Items.Cast<object>().Any(item =>
                item.ToString().Contains("Hello from the original screen"))
                && Control<TextBox>(page, "chatMessageTextBox").Text.Length == 0);
            AwaitUi(secondChat.OpenChatChannel(waiting.MatchId));
            AwaitUi(secondChat.SendChatMessage(waiting.MatchId, "Reply from the second player"));
            PumpUntil(() => messages.Items.Count == 2);

            const int MuteDurationSeconds = 2;
            firstChat.OnPlayerMuted(new Proxy.ChatMuteNotification
            {
                MatchId = waiting.MatchId,
                EndsAtUtc = DateTime.UtcNow.AddSeconds(MuteDurationSeconds)
            });
            TextBox composer = Control<TextBox>(page, "chatMessageTextBox");
            PumpUntil(() => !composer.IsEnabled && !send.IsEnabled);
            PumpUntil(() => composer.IsEnabled && send.IsEnabled);

            // Assert: preserve the same screen and render both supported languages.
            foreach (string culture in new[] { "en-US", "es-MX" })
            {
                LocalizationManager.Instance.CultureName = culture;
                page.UpdateLayout();
                PumpUntil(() => messages.Items.Count == 2);
                Assert.AreEqual(string.Format(LocalizationManager.Instance["Game_ScoreFormat"], 4),
                    Control<TextBlock>(page, "creatorScoreTextBlock").Text);
                Assert.AreEqual(true, messages.Items.Cast<object>().Any(item =>
                    item.ToString().Contains("Reply from the second player")));
                SavePreview(page, culture);
            }
            AssertSystemOpponentLocalization(window, firstLobby, first, firstChat);
            Console.WriteLine("Passed original GamePage rendering and two-way UI chat in en-US and es-MX.");
        }
        finally
        {
            window.Close();
            SynchronizationContext.SetSynchronizationContext(null);
            sessions.CloseSession(beta.SessionToken).GetAwaiter().GetResult();
            sessions.CloseSession(alpha.SessionToken).GetAwaiter().GetResult();
            app.Shutdown();
        }
    }

    private static void AssertSystemOpponentLocalization(Window window,
        MatchLobbyClient lobby, GameplayClient gameplay, MatchChatClient chat)
    {
        FrameworkElement previousPage = (FrameworkElement)((Frame)window.Content).Content;
        window.Content = null;
        PumpUntil(() => !previousPage.IsLoaded);
        Assert.AreEqual(MatchStatus.Success,
            gameplay.ForfeitMatch(lobby.ReadCurrentLobby().MatchId).GetAwaiter().GetResult());
        Assert.AreEqual(MatchStatus.Success, lobby.CreateMatch(40).GetAwaiter().GetResult());
        long matchId = lobby.ReadCurrentLobby().MatchId;
        Assert.AreEqual(MatchStatus.Success,
            lobby.StartSystemMatch(matchId).GetAwaiter().GetResult());
        GamePage page = new GamePage(gameplay, chat, matchId, null);
        window.Content = new Frame { Content = page };
        PumpUntil(() => Control<TextBlock>(page, "creatorNameTextBlock").Text == "ReviewAlpha");
        MatchStateSnapshot state = gameplay.ReadMatchState(matchId);
        AwaitUi(gameplay.PlacePiece(new MatchPlacement
        {
            MatchId = matchId,
            ExpectedVersion = state.Version,
            Row = 2,
            Column = 3
        }));
        foreach (string culture in new[] { "en-US", "es-MX" })
        {
            LocalizationManager.Instance.CultureName = culture;
            string systemName = culture == "en-US" ? "System" : "Sistema";
            PumpUntil(() => Control<TextBlock>(page, "opponentNameTextBlock").Text == systemName);
            Assert.AreEqual(string.Format(
                LocalizationManager.Instance["Game_CurrentTurnFormat"], systemName),
                Control<TextBlock>(page, "turnTextBlock").Text);
            Assert.AreEqual(Visibility.Hidden,
                Control<FrameworkElement>(page, "chatPanel").Visibility);
            Assert.AreEqual(Visibility.Hidden,
                Control<TextBlock>(page, "opponentConnectionTextBlock").Visibility);
        }
    }

    private static AuthenticatedAccountSession Authenticate(
        SessionClient sessions,
        string login,
        string verificationCode)
    {
        SessionResult<AuthenticatedAccountSession> result = sessions.CompleteLogin(
            login,
            verificationCode).GetAwaiter().GetResult();
        Assert.AreEqual(SessionStatus.Success, result.Status);
        return TestAssert.IsNotNull(result.Value);
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
