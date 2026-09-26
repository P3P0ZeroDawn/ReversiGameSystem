namespace ReversiGameSystem.Services;

internal enum BoardBackgroundSaveResult
{
    Success,
    ServerConnectionFailed,
    PersistenceError
}

internal interface IBoardBackgroundService
{
    BoardBackgroundSaveResult SaveForCurrentPlayer(string imagePath);
}

/// <summary>
/// Holds the account preference boundary until server storage is implemented.
/// </summary>
internal sealed class BoardBackgroundService : IBoardBackgroundService
{
    public BoardBackgroundSaveResult SaveForCurrentPlayer(string imagePath)
    {
        // TODO: Upload the image as this account's private board appearance preference.
        return BoardBackgroundSaveResult.ServerConnectionFailed;
    }
}
