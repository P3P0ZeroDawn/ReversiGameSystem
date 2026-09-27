using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Leaderboards;

/// <summary>
/// Defines the supported leaderboard result code values.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum LeaderboardResultCode
{
    /// <summary>
    /// Represents success.
    /// </summary>
    [EnumMember]
    Success,

    /// <summary>
    /// Represents invalid request.
    /// </summary>
    [EnumMember]
    InvalidRequest,

    /// <summary>
    /// Represents service unavailable.
    /// </summary>
    [EnumMember]
    ServiceUnavailable
}
