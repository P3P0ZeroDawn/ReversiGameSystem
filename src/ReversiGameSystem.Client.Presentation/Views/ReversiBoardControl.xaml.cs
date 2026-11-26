using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ReversiGameSystem.Game;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

public partial class ReversiBoardControl : UserControl
{
    private const double PieceSize = 46;
    private const double ProfilePictureDiameterRatio = 0.65;
    private const double ProfilePieceOutlineThickness = 1;
    private const double GridDrawingSize = 470;
    private const double DarkGridStroke = 3;
    private const double LightGridStroke = 1;
    private const double OuterPieceStroke = 3;
    private const double InnerPieceStroke = 3;
    private const double PieceImageInset = 6;
    private const string EmbeddedImagePrefix =
        "pack://application:,,,/ReversiGameSystem.Client.Presentation;component";

    public ReversiBoardControl()
    {
        InitializeComponent();
    }

    internal event EventHandler<BoardSquareSelectedEventArgs>? SquareSelected;

    internal DiscColor SystemPlayerColor { get; set; } = DiscColor.Empty;

    internal void ShowBoard(ReversiBoard board)
    {
        ShowBoard(board, DiscColor.Black);
    }

    internal void ShowBoard(ReversiBoard board, DiscColor localPlayerColor)
    {
        ApplyBoardBackground();
        RenderPieces(board, localPlayerColor);
        RenderInteraction(Array.Empty<BoardPosition>(), false);
    }

    internal void ShowInteractiveBoard(
        ReversiBoard board,
        DiscColor localPlayerColor,
        IReadOnlyList<BoardPosition> validMoves)
    {
        ApplyBoardBackground();
        RenderPieces(board, localPlayerColor);
        RenderInteraction(validMoves, true);
    }

    private void ApplyBoardBackground()
    {
        BitmapSource? customBackground = UserPreferencesState.BoardBackgroundPreview;
        bool useCustomBackground = UserPreferencesState.UseCustomBoardBackground
            && customBackground != null;
        boardBackgroundImage.Source = useCustomBackground
            ? customBackground
            : CreateImageSource("/Assets/Images/DefaultBoardBackground.png");
        contrastGridView.Visibility = useCustomBackground
            ? Visibility.Visible
            : Visibility.Collapsed;
        boardMaskImage.Visibility = contrastGridView.Visibility;
        contrastGridCanvas.Children.Clear();
        if (useCustomBackground)
        {
            for (int index = 0; index <= ReversiBoard.Size; index++)
            {
                double position = index * GridDrawingSize / ReversiBoard.Size;
                DrawContrastLine(new Point(position, 0), new Point(position, GridDrawingSize));
                DrawContrastLine(new Point(0, position), new Point(GridDrawingSize, position));
            }
        }
    }

    private void DrawContrastLine(Point start, Point end)
    {
        contrastGridCanvas.Children.Add(new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = Brushes.Black,
            StrokeThickness = DarkGridStroke
        });
        contrastGridCanvas.Children.Add(new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = Brushes.White,
            StrokeThickness = LightGridStroke
        });
    }

    private void RenderPieces(ReversiBoard board, DiscColor localPlayerColor)
    {
        piecesGrid.Children.Clear();
        piecesGrid.RowDefinitions.Clear();
        piecesGrid.ColumnDefinitions.Clear();
        for (int index = 0; index < ReversiBoard.Size; index++)
        {
            piecesGrid.RowDefinitions.Add(new RowDefinition());
            piecesGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        for (int row = 0; row < ReversiBoard.Size; row++)
        {
            for (int column = 0; column < ReversiBoard.Size; column++)
            {
                DiscColor color = board[row, column];
                if (color == DiscColor.Empty)
                {
                    continue;
                }

                FrameworkElement piece = UserPreferencesState.UseProfilePicturesOnPieces
                    ? CreateProfilePiece(color, localPlayerColor)
                    : CreateDefaultPiece(color);
                Grid.SetRow(piece, row);
                Grid.SetColumn(piece, column);
                piecesGrid.Children.Add(piece);
            }
        }
    }

    private void RenderInteraction(
        IReadOnlyList<BoardPosition> validMoves,
        bool enableInput)
    {
        interactionGrid.Children.Clear();
        interactionGrid.RowDefinitions.Clear();
        interactionGrid.ColumnDefinitions.Clear();
        interactionGrid.IsHitTestVisible = enableInput;
        for (int index = 0; index < ReversiBoard.Size; index++)
        {
            interactionGrid.RowDefinitions.Add(new RowDefinition());
            interactionGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        HashSet<BoardPosition> validMoveSet =
            new HashSet<BoardPosition>(validMoves);
        for (int row = 0; row < ReversiBoard.Size; row++)
        {
            for (int column = 0; column < ReversiBoard.Size; column++)
            {
                BoardPosition position = new BoardPosition(row, column);
                bool isValid = validMoveSet.Contains(position);
                Border hitArea = new Border
                {
                    Background = Brushes.Transparent,
                    Cursor = isValid ? Cursors.Hand : Cursors.Arrow,
                    Tag = position
                };
                if (isValid)
                {
                    hitArea.Child = new Ellipse
                    {
                        Width = 18,
                        Height = 18,
                        Fill = new SolidColorBrush(Color.FromArgb(150, 255, 214, 75)),
                        Stroke = new SolidColorBrush(Color.FromRgb(117, 88, 0)),
                        StrokeThickness = 2,
                        IsHitTestVisible = false
                    };
                }

                AutomationProperties.SetName(hitArea, position.ToNotation());
                hitArea.MouseLeftButtonUp += OnSquareMouseLeftButtonUp;
                Grid.SetRow(hitArea, row);
                Grid.SetColumn(hitArea, column);
                interactionGrid.Children.Add(hitArea);
            }
        }
    }

    private void OnSquareMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is BoardPosition position)
        {
            SquareSelected?.Invoke(this, new BoardSquareSelectedEventArgs(position));
        }
    }

    private static FrameworkElement CreateDefaultPiece(DiscColor color)
    {
        Grid piece = CreatePieceFrame(color);
        piece.Children.Add(new Image
        {
            Margin = new Thickness(PieceImageInset),
            Stretch = Stretch.Uniform,
            Source = CreateImageSource(color == DiscColor.Black
                ? "/Assets/Images/DefaultBlackPiece.png"
                : "/Assets/Images/DefaultWhitePiece.png")
        });
        return piece;
    }

    private FrameworkElement CreateProfilePiece(
        DiscColor color,
        DiscColor localPlayerColor)
    {
        ImageSource source;
        if (color == SystemPlayerColor)
        {
            source = CreateImageSource("/Assets/Images/SystemProfilePicture.png");
        }
        else if (color == localPlayerColor && ClientRuntime.CurrentSession.ProfilePicture != null)
        {
            source = ClientRuntime.CurrentSession.ProfilePicture;
        }
        else
        {
            source = CreateImageSource("/Assets/Images/DefaultProfilePicture.png");
        }
        Brush discColor = color == DiscColor.Black ? Brushes.Black : Brushes.White;
        Brush outlineColor = color == DiscColor.Black ? Brushes.White : Brushes.Black;
        double pictureInset = PieceSize * (1 - ProfilePictureDiameterRatio) / 2;
        Grid piece = new Grid { Width = PieceSize, Height = PieceSize };
        piece.Children.Add(new Ellipse
        {
            Fill = discColor,
            Stroke = outlineColor,
            StrokeThickness = ProfilePieceOutlineThickness
        });
        piece.Children.Add(new Ellipse
        {
            Margin = new Thickness(pictureInset),
            Fill = new ImageBrush(source) { Stretch = Stretch.UniformToFill }
        });
        piece.Children.Add(new Image
        {
            Margin = new Thickness(pictureInset),
            Source = CreateImageSource("/Assets/Images/PieceMask.png"),
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        });
        return piece;
    }

    private static Grid CreatePieceFrame(DiscColor color)
    {
        Brush mainColor = color == DiscColor.Black ? Brushes.Black : Brushes.White;
        Brush contrastColor = color == DiscColor.Black ? Brushes.White : Brushes.Black;
        Grid piece = new Grid { Width = PieceSize, Height = PieceSize };
        piece.Children.Add(new Ellipse
        {
            Fill = mainColor,
            Stroke = contrastColor,
            StrokeThickness = OuterPieceStroke
        });
        piece.Children.Add(new Ellipse
        {
            Margin = new Thickness(OuterPieceStroke),
            Stroke = mainColor,
            StrokeThickness = InnerPieceStroke
        });
        return piece;
    }

    private static BitmapImage CreateImageSource(string path)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(EmbeddedImagePrefix + path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}

internal sealed class BoardSquareSelectedEventArgs : EventArgs
{
    internal BoardSquareSelectedEventArgs(BoardPosition position)
    {
        Position = position;
    }

    internal BoardPosition Position { get; }
}
