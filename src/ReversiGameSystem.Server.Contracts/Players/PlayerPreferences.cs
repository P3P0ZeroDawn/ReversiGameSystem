using System.Runtime.Serialization;

namespace ReversiGameSystem.Server.Contracts.Players;

/// <summary>
/// Contains the editable presentation preferences of an authenticated account.
/// </summary>
[DataContract(Namespace = ContractNamespaces.Data)]
public sealed class PlayerPreferences
{
    /// <summary>
    /// Gets or sets the supported account locale code.
    /// </summary>
    [DataMember(Order = 1, IsRequired = true)]
    public string PreferredLocaleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the supported time zone identifier, or null to use the client local zone.
    /// </summary>
    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets the piece style code: STANDARD or PROFILE_PICTURES.
    /// </summary>
    [DataMember(Order = 3, IsRequired = true)]
    public string PieceStyleCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to use the custom background when one is stored.
    /// </summary>
    [DataMember(Order = 4, IsRequired = true)]
    public bool UseCustomBoardBackground { get; set; }
}
