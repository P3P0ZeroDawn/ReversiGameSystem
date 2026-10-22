using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Faults;

/// <summary>
/// Identifies a recognized preference rejection.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public enum PreferencesFailure
{
    /// <summary>
    /// The editable preference object is absent.
    /// </summary>
    [EnumMember]
    MissingPreferences,

    /// <summary>
    /// The locale is absent or unsupported.
    /// </summary>
    [EnumMember]
    InvalidLocale,

    /// <summary>
    /// The specified time zone identifier is empty or unsupported.
    /// </summary>
    [EnumMember]
    InvalidTimeZone,

    /// <summary>
    /// The piece style is absent or unsupported.
    /// </summary>
    [EnumMember]
    InvalidPieceStyle,

    /// <summary>
    /// The image is absent or has invalid content, format, size or dimensions.
    /// </summary>
    [EnumMember]
    InvalidImage,

    /// <summary>
    /// The requesting or affected account is no longer active.
    /// </summary>
    [EnumMember]
    InactiveAccount,
}
