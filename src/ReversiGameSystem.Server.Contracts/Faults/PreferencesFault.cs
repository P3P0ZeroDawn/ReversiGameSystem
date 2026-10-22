using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Describes a recognized player preference rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PreferencesFault
{
    /// <summary>
    /// Gets or sets the recognized rejection reason.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public PreferencesFailure Reason { get; set; }
}
