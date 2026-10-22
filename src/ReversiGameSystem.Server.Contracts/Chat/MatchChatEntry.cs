using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Chat;

/// <summary>
/// Represents a stored chat message identified by its match and message number.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class MatchChatEntry
{
    /// <summary>
    /// Gets or sets the stable match identifier.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public long MatchId { get; set; }

    /// <summary>
    /// Gets or sets the stable positive message number within its match.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public int MessageNumber { get; set; }

    /// <summary>
    /// Gets or sets the registered sender seat within the match.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public int SenderSeatNumber { get; set; }

    /// <summary>
    /// Gets or sets the trimmed message text accepted by the server.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC message storage time confirmed by the server.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public DateTime SentAtUtc { get; set; }
}
