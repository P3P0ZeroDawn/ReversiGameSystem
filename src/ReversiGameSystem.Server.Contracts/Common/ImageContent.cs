using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Common;

/// <summary>
/// Contains an image media type and its encoded bytes.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ImageContent
{
    /// <summary>
    /// Gets or sets the declared media type of the image.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encoded image bytes.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
