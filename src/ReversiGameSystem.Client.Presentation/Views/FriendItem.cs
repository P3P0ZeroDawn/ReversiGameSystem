using System.Windows.Media;
using ReversiGameSystem.Localization;
using Proxy = ReversiGameSystem.ServerProxy;

namespace ReversiGameSystem.Views;

internal sealed class FriendItem
{
    internal FriendItem(Proxy.FriendSummary friend)
    {
        AccountId = friend.Account.AccountId;
        Username = friend.Account.Username;
        IsOnline = friend.IsOnline;
    }

    public long AccountId { get; }
    public string Username { get; }
    public bool IsOnline { get; }
    public string PresenceText => LocalizationManager.Instance[
        IsOnline ? "Friends_Online" : "Friends_Offline"];
    public Brush PresenceBrush => IsOnline ? Brushes.ForestGreen : Brushes.Gray;
}
