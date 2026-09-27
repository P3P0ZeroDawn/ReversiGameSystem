using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Media;

/// <summary>
/// Represents profile image upload dto.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ProfileImageUploadDto
{
    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string MediaType { get; set; } = string.Empty;
}
