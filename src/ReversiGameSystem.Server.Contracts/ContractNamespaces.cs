namespace ReversiGameSystem.Server.Contracts;

/// <summary>
/// Defines the stable namespaces published by the WCF boundary.
/// </summary>
public static class ContractNamespaces
{
    /// <summary>
    /// Identifies service contracts.
    /// </summary>
    public const string Services = "urn:reversi:services:v1";

    /// <summary>
    /// Identifies serialized data contracts.
    /// </summary>
    public const string Data = "urn:reversi:contracts:v1";

    /// <summary>
    /// Identifies diagnostic fault contracts.
    /// </summary>
    public const string Diagnostics = "urn:reversi:diagnostics:v1";
}
