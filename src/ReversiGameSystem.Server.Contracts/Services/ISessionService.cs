using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines verified account access, guest sessions and explicit session closure.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface ISessionService
{
    /// <summary>
    /// Verifies active account credentials and requests a one-use login code.
    /// </summary>
    /// <param name="loginIdentifier">
    /// The username or email address identifying the account.
    /// </param>
    /// <param name="password">
    /// The account password to verify through the protected transport.
    /// </param>
    /// <returns>
    /// The UTC expiration of the code accepted for delivery; the code itself is not returned.
    /// </returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public DateTime RequestLoginCode(string loginIdentifier, string password);

    /// <summary>
    /// Consumes a valid login code and creates an authenticated session.
    /// </summary>
    /// <param name="loginIdentifier">
    /// The username or email address identifying the account.
    /// </param>
    /// <param name="verificationCode">
    /// The one-use code for the relevant access or recovery process.
    /// </param>
    /// <returns>
    /// The authenticated session issued after verification succeeds.
    /// </returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public AuthenticatedSession CompleteLogin(string loginIdentifier, string verificationCode);

    /// <summary>
    /// Validates and reserves a guest name and opens a guest session.
    /// </summary>
    /// <param name="displayName">
    /// The proposed guest name without the reserved Guest- prefix.
    /// </param>
    /// <returns>
    /// The guest session with its confirmed name and expiration.
    /// </returns>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public GuestSession OpenGuestSession(string displayName);

    /// <summary>
    /// Revokes the identified session or confirms that it is already inactive.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void CloseSession(string sessionToken);
}
