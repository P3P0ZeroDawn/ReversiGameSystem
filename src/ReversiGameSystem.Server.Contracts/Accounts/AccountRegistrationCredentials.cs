using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Represents the information required to register an account.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class AccountRegistrationCredentials
{
    /// <summary>
    /// Gets or sets the requested username.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account email address.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plaintext password provided in transit.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial locale code.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional initial profile image.
    /// </summary>
    [DataMember(Order = 5, EmitDefaultValue = false)]
    public ProfileImageContent? ProfileImage { get; set; }
}
