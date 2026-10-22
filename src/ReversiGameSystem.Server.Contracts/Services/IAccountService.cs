using System;
using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Accounts;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines account registration, password management and deactivation.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IAccountService
{
    /// <summary>
    /// Validates the proposed identity and requests a one-use registration code.
    /// </summary>
    /// <param name="username">
    /// The proposed public account username.
    /// </param>
    /// <param name="emailAddress">
    /// The account email address for registration or recovery.
    /// </param>
    /// <returns>
    /// The UTC expiration of the code accepted for delivery.
    /// </returns>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(AccountFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public DateTime RequestRegistrationCode(string username, string emailAddress);

    /// <summary>
    /// Consumes a valid registration code and saves the new account without opening a session.
    /// </summary>
    /// <param name="account">
    /// The complete verified registration data and optional profile image.
    /// </param>
    /// <returns>
    /// The public identity of the account whose registration was saved.
    /// </returns>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(AccountFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public AccountReference CompleteRegistration(NewAccount account);

    /// <summary>
    /// Verifies the current password and changes the authenticated account password.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="currentPassword">
    /// The current account password used to confirm ownership.
    /// </param>
    /// <param name="newPassword">
    /// The proposed new password, subject to the server password policy.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(AccountFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void ChangePassword(string sessionToken, string currentPassword, string newPassword);

    /// <summary>
    /// Verifies a recovery code and changes the active account password.
    /// </summary>
    /// <param name="emailAddress">
    /// The account email address for registration or recovery.
    /// </param>
    /// <param name="verificationCode">
    /// The one-use code for the relevant access or recovery process.
    /// </param>
    /// <param name="newPassword">
    /// The proposed new password, subject to the server password policy.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(AccountFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void ResetPassword(string emailAddress, string verificationCode, string newPassword);

    /// <summary>
    /// Confirms account ownership and deactivates the account while preserving its history.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="currentPassword">
    /// The current account password used to confirm ownership.
    /// </param>
    /// <exception cref="FaultException{AccountFault}">
    /// A declared account rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(AccountFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void DeactivateAccount(string sessionToken, string currentPassword);
}
