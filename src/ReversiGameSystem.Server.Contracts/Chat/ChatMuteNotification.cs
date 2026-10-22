using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Chat;

/// <summary>
/// Describes a confirmed account mute delivered through an authorized chat.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ChatMuteNotification
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the stable mute reason code REPEATED_VIOLATIONS.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string ReasonCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC start of the confirmed mute.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public DateTime StartsAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC end of the confirmed mute.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public DateTime EndsAtUtc { get; set; }
}
