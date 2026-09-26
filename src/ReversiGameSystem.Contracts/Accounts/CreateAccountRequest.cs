using System.Runtime.Serialization;
using ReversiGameSystem.Contracts.Media;

namespace ReversiGameSystem.Contracts.Accounts;

[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class CreateAccountRequest
{
    [DataMember(Order = 1, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    [DataMember(Order = 3, IsRequired = true)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 4, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    [DataMember(Order = 5, EmitDefaultValue = false)]
    public ProfileImageUploadDto? ProfileImage { get; set; }
}
