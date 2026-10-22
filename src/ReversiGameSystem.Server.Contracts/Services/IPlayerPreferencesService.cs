using System.ServiceModel;
using ReversiGameSystem.Server.Contracts.Common;
using ReversiGameSystem.Server.Contracts.Faults;
using ReversiGameSystem.Server.Contracts.Players;

namespace ReversiGameSystem.Server.Contracts.Services;

/// <summary>
/// Defines persistent preferences and board background operations for an account.
/// </summary>
[ServiceContract(Namespace = ContractNamespaces.Services)]
public interface IPlayerPreferencesService
{
    /// <summary>
    /// Retrieves the authenticated account's effective presentation preferences.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The persisted editable preferences of the account.
    /// </returns>
    /// <exception cref="FaultException{PreferencesFault}">
    /// A declared preferences rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(PreferencesFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public PlayerPreferences GetPlayerPreferences(string sessionToken);

    /// <summary>
    /// Validates and replaces the complete set of editable account preferences.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="preferences">
    /// The complete set of editable account preferences.
    /// </param>
    /// <exception cref="FaultException{PreferencesFault}">
    /// A declared preferences rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(PreferencesFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void UpdatePlayerPreferences(string sessionToken, PlayerPreferences preferences);

    /// <summary>
    /// Retrieves the stored background regardless of whether its use is enabled.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <returns>
    /// The stored background, or null when no custom image is stored.
    /// </returns>
    /// <exception cref="FaultException{PreferencesFault}">
    /// A declared preferences rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(PreferencesFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public ImageContent? GetBoardBackground(string sessionToken);

    /// <summary>
    /// Stores a validated board background and enables its use for the account.
    /// </summary>
    /// <param name="sessionToken">
    /// The opaque session token identifying the caller.
    /// </param>
    /// <param name="image">
    /// The image media type and encoded content to validate and store.
    /// </param>
    /// <exception cref="FaultException{PreferencesFault}">
    /// A declared preferences rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{SessionFault}">
    /// A declared session rejection occurred.
    /// </exception>
    /// <exception cref="FaultException{ServiceFault}">
    /// A controlled technical failure prevented the server from completing the operation.
    /// </exception>
    [OperationContract]
    [FaultContract(typeof(PreferencesFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(SessionFault), Namespace = ContractNamespaces.Data)]
    [FaultContract(typeof(ServiceFault), Namespace = ContractNamespaces.Diagnostics)]
    public void ChangeBoardBackground(string sessionToken, ImageContent image);
}
