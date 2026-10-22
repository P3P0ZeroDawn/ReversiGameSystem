using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a rejected message or an active account mute.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum ChatFailure
{
    /// <summary>
    /// The message is absent or empty after trimming whitespace.
    /// </summary>
    [EnumMember]
    EmptyMessage,

    /// <summary>
    /// The trimmed message exceeds 200 UTF-16 code units.
    /// </summary>
    [EnumMember]
    MessageTooLong,

    /// <summary>
    /// Moderation rejected the message without applying a new mute.
    /// </summary>
    [EnumMember]
    MessageBlocked,

    /// <summary>
    /// An account mute is active, including a mute just applied for violations.
    /// </summary>
    [EnumMember]
    PlayerMuted,
}
