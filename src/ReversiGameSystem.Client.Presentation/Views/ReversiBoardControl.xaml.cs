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

/// <summary>
/// Renders a Reversi board with the current player's local appearance preferences.
/// </summary>
public partial class ReversiBoardControl : UserControl
{
    private const double PieceSize = 46;

    /// <summary>
    /// Initializes the reusable board.
    /// </summary>
    public ReversiBoardControl()
    {
        InitializeComponent();
    }

    internal event EventHandler<BoardSquareSelectedEventArgs>? SquareSelected;

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
        boardMaskImage.Visibility = useCustomBackground
            ? Visibility.Visible
            : Visibility.Collapsed;
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
        return new Image
        {
            Width = PieceSize,
            Height = PieceSize,
            Stretch = Stretch.Uniform,
            Source = CreateImageSource(color == DiscColor.Black
                ? "/Assets/Images/DefaultBlackPiece.png"
                : "/Assets/Images/DefaultWhitePiece.png")
        };
    }

    private static FrameworkElement CreateProfilePiece(
        DiscColor color,
        DiscColor localPlayerColor)
    {
        ImageSource source = color == localPlayerColor
            && ClientRuntime.CurrentSession.ProfilePicture != null
                ? ClientRuntime.CurrentSession.ProfilePicture
                : CreateImageSource("/Assets/Images/DefaultProfilePicture.png");
        Grid pieceGrid = new Grid { Width = PieceSize, Height = PieceSize };
        pieceGrid.Children.Add(new Ellipse
        {
            Fill = new ImageBrush(source) { Stretch = Stretch.UniformToFill },
            Stroke = new SolidColorBrush(color == DiscColor.Black
                ? Color.FromRgb(35, 35, 35)
                : Colors.White),
            StrokeThickness = 4
        });
        pieceGrid.Children.Add(new Image
        {
            Source = CreateImageSource("/Assets/Images/PieceMask.png"),
            Stretch = Stretch.Fill,
            IsHitTestVisible = false
        });
        return pieceGrid;
    }

    private static BitmapImage CreateImageSource(string path)
    {
        return new BitmapImage(new Uri(
            "/ReversiGameSystem.Client.Presentation;component" + path, UriKind.Relative));
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
