using System.Runtime.Serialization;
using ReversiGameSystem.Contracts.Media;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Represents create account request.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class CreateAccountRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred locale code.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile image.
    /// </summary>
    [DataMember(Order = 5, EmitDefaultValue = false)]
    public ProfileImageUploadDto? ProfileImage { get; set; }
}
