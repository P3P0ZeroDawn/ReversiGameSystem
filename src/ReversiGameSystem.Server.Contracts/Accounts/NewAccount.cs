using System.Runtime.Serialization;
using ReversiGameSystem.Server.Contracts.Common;

namespace ReversiGameSystem.Server.Contracts.Accounts;

/// <summary>
/// Contains the verified registration data for a new player account.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class NewAccount
{
    /// <summary>
    /// Gets or sets the one-use registration code, preserving leading zeroes.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string VerificationCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proposed public account username.
    /// </summary>
    [DataMember(Order = 2, IsRequired = true)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address associated with the new account.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account password supplied through the protected transport.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported account locale code.
    /// </summary>
    [DataMember(Order = 5, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional custom profile image.
    /// </summary>
    [DataMember(Order = 6, EmitDefaultValue = false)]
    public ImageContent? ProfileImage { get; set; }
}
