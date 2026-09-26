using System;
using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Media;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class ProfileImageUploadDto
{
    [DataMember(Order = 1, IsRequired = true)]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    [DataMember(Order = 2, IsRequired = true)]
    public string MediaType { get; set; } = string.Empty;
}
