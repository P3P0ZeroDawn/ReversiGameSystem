using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Common;

/// <summary>
/// Represents a controlled technical failure returned by a WCF operation.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Diagnostics)]
public sealed class ServiceFault
{
    /// <summary>
    /// Gets or sets the stable technical failure code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier used to correlate distributed diagnostic events.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether retrying may be reasonable.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public bool IsTransient { get; set; }
}
