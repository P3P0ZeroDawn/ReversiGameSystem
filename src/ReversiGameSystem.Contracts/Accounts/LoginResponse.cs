using System.Runtime.Serialization;

namespace ReversiGameSystem.Contracts.Accounts;

/// <summary>
/// Represents login response.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public LoginResultCode ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the session.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public AuthenticatedSessionDto? Session { get; set; }
}
