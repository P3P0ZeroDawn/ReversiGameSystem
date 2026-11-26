using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;
using ReversiGameSystem.Views;

namespace ReversiGameSystem.Client.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ReversiBoardControlTests
{
    [STATestMethod]
    public void ShowBoard_ProfilePiecesWithoutPhoto_LoadsEmbeddedDefaultPicture()
    {
        UserPreferencesState.ApplyPreferences(new PlayerPreferences
        {
            PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle
        });
        try
        {
            ReversiBoardControl control = new ReversiBoardControl();
            control.ShowBoard(ReversiBoard.CreateInitial());
            Grid pieces = (Grid)control.FindName("piecesGrid");
            Grid firstPiece = (Grid)pieces.Children[0];
            Ellipse disc = (Ellipse)firstPiece.Children[1];
            ImageBrush pictureBrush = (ImageBrush)disc.Fill;
            BitmapSource picture = (BitmapSource)pictureBrush.ImageSource;
            byte[] pixel = new byte[4];

            picture.CopyPixels(new Int32Rect(0, 0, 1, 1), pixel, pixel.Length, 0);

            Assert.IsTrue(picture.PixelWidth > 0);
        }
        finally
        {
            UserPreferencesState.Reset();
        }
    }
    [STATestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)255)]
    public void ShowBoard_CustomBackground_KeepsGridContrast(byte backgroundLevel)
    {
        const int BoardPixels = 500;
        const int PixelBytes = 4;
        const double Resolution = 96;
        BitmapSource background = BitmapSource.Create(1, 1, Resolution, Resolution,
            PixelFormats.Bgra32, null,
            new byte[] { backgroundLevel, backgroundLevel, backgroundLevel, 255 }, PixelBytes);
        background.Freeze();
        UserPreferencesState.SetBoardBackground(background);
        UserPreferencesState.ApplyPreferences(new PlayerPreferences
        {
            UseCustomBoardBackground = true
        });
        try
        {
            ReversiBoardControl control = new ReversiBoardControl();
            control.ShowBoard(ReversiBoard.CreateInitial());
            control.Measure(new Size(BoardPixels, BoardPixels));
            control.Arrange(new Rect(0, 0, BoardPixels, BoardPixels));
            control.UpdateLayout();
            RenderTargetBitmap rendered = new RenderTargetBitmap(
                BoardPixels, BoardPixels, Resolution, Resolution, PixelFormats.Pbgra32);
            rendered.Render(control);
            const int SampleWidth = 5;
            byte[] pixels = new byte[SampleWidth * PixelBytes];

            rendered.CopyPixels(new Int32Rect(72, 30, SampleWidth, 1),
                pixels, pixels.Length, 0);
            byte minimum = byte.MaxValue;
            byte maximum = byte.MinValue;
            for (int offset = 0; offset < pixels.Length; offset += PixelBytes)
            {
                minimum = System.Math.Min(minimum, pixels[offset]);
                maximum = System.Math.Max(maximum, pixels[offset]);
            }

            const int MinimumContrast = 100;
            Assert.IsTrue(maximum - minimum >= MinimumContrast);
        }
        finally
        {
            UserPreferencesState.Reset();
        }
    }

    [STATestMethod]
    public void ShowBoard_StandardPieces_HasLightAndDarkBorders()
    {
        UserPreferencesState.Reset();
        ReversiBoardControl control = new ReversiBoardControl();

        control.ShowBoard(ReversiBoard.CreateInitial());
        Grid pieces = (Grid)control.FindName("piecesGrid");
        Grid piece = (Grid)pieces.Children[0];
        Ellipse outerBorder = (Ellipse)piece.Children[0];
        Ellipse innerBorder = (Ellipse)piece.Children[1];

        Assert.AreEqual(Colors.Black, ((SolidColorBrush)outerBorder.Stroke).Color);
        Assert.AreEqual(Colors.White, ((SolidColorBrush)innerBorder.Stroke).Color);
    }
    [STATestMethod]
    public void ShowBoard_SystemWithProfilePieces_UsesRobotAvatar()
    {
        UserPreferencesState.ApplyPreferences(new PlayerPreferences
        {
            PieceStyleCode = PlayerPreferences.ProfilePicturesPieceStyle
        });
        try
        {
            ReversiBoardControl control = new ReversiBoardControl
            {
                SystemPlayerColor = DiscColor.White
            };
            control.ShowBoard(ReversiBoard.CreateInitial(), DiscColor.Black);
            Grid pieces = (Grid)control.FindName("piecesGrid");
            Grid systemPiece = (Grid)pieces.Children[0];
            Ellipse picture = (Ellipse)systemPiece.Children[1];
            BitmapImage avatar = (BitmapImage)((ImageBrush)picture.Fill).ImageSource;

            StringAssert.EndsWith(avatar.UriSource.AbsoluteUri, "/SystemProfilePicture.png");
        }
        finally
        {
            UserPreferencesState.Reset();
        }
    }

    [STATestMethod]
    public void ShowBoard_CustomBackground_PreservesOriginalBoardMask()
    {
        const double Resolution = 96;
        BitmapSource background = BitmapSource.Create(1, 1, Resolution, Resolution,
            PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 255 }, 4);
        background.Freeze();
        UserPreferencesState.SetBoardBackground(background);
        UserPreferencesState.ApplyPreferences(new PlayerPreferences
        {
            UseCustomBoardBackground = true
        });
        try
        {
            ReversiBoardControl control = new ReversiBoardControl();

            control.ShowBoard(ReversiBoard.CreateInitial());
            Image originalMask = (Image)control.FindName("boardMaskImage");

            Assert.AreEqual(Visibility.Visible, originalMask.Visibility);
        }
        finally
        {
            UserPreferencesState.Reset();
        }
    }
}
