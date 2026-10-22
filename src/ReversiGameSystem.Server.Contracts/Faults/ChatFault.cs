using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a rejected chat message and any applicable mute expiration.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ChatFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public ChatFailure Reason { get; set; }

    /// <summary>
    /// Gets or sets the mute expiration required for PlayerMuted and absent for other reasons.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public DateTime? MuteEndsAtUtc { get; set; }
}
