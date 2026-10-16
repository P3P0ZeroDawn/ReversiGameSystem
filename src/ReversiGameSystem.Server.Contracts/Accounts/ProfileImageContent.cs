using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents profile image content crossing the WCF boundary.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ProfileImageContent
{
    /// <summary>
    /// Gets or sets the image media type.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image bytes.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public byte[] Content { get; set; } = System.Array.Empty<byte>();
}
