using System.Windows.Media;
using ReversiGameSystem.Localization;
using ReversiGameSystem.Services;

namespace ReversiGameSystem.Views;

internal sealed class FriendItem
{
    internal FriendItem(FriendSummary friend)
    {
        AccountId = friend.AccountId;
        Username = friend.Username;
        IsOnline = friend.IsOnline;
    }

    public long AccountId { get; }
    public string Username { get; }
    public bool IsOnline { get; }
    public string PresenceText
    {
        get
        {
            return LocalizationManager.Instance[
                IsOnline ? "Friends_Online" : "Friends_Offline"];
        }
    }

    public Brush PresenceBrush
    {
        get { return IsOnline ? Brushes.ForestGreen : Brushes.Gray; }
    }
}
