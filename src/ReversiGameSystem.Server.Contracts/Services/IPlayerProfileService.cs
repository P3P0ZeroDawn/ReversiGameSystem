using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines public profile lookup and authorized username and profile image changes.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerProfileService
{
    /// <summary>
    /// Retrieves the public identity, image availability and statistics of an authorized account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="targetAccountId">
    /// The positive target account identifier, or null for the caller's own account.
    /// </param>
    /// <returns>
    /// The requested public player profile and server-calculated statistics.
    /// </returns>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(ProfileFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public PlayerProfile GetPlayerProfile(string sessionToken, long? targetAccountId);

    /// <summary>
    /// Retrieves the custom profile image of an authorized account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="targetAccountId">
    /// The positive target account identifier, or null for the caller's own account.
    /// </param>
    /// <returns>
    /// The stored image, or null when the existing account has no custom image.
    /// </returns>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(ProfileFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public ImageContent? GetProfileImage(string sessionToken, long? targetAccountId);

    /// <summary>
    /// Changes the authenticated account username after validating its availability.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="username">
    /// The proposed public account username.
    /// </param>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(ProfileFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void ChangeUsername(string sessionToken, string username);

    /// <summary>
    /// Validates and replaces the authenticated account profile image.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="image">
    /// The image media type and encoded content to validate and store.
    /// </param>
    /// <exception cref="FaultException{ProfileFault}">
    /// A declared profile rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(ProfileFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void UpdateProfileImage(string sessionToken, ImageContent image);
}
