using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Chat;

namespace ReversiGameSystem.Server.Contracts.Callbacks;

/// <summary>
/// Receives stored chat messages and mutes affecting the receiving account.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IMatchChatServiceCallback
{
    /// <summary>
    /// Delivers a stored message to an authorized conversation participant.
    /// </summary>
    /// <param name="message">
    /// The stored message with its stable match and message number.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnChatMessageReceived(MatchChatEntry message);

    /// <summary>
    /// Notifies the affected account of a confirmed account mute through its chat channel.
    /// </summary>
    /// <param name="mute">
    /// The confirmed mute reason and UTC validity period for the receiving account.
    /// </param>
    [OperationContract(IsOneWay = true)]
    public void OnPlayerMuted(ChatMuteNotification mute);
}
