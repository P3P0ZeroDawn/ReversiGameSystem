using System.Data.Entity;
using System.Data.Entity.SqlServer;

namespace ReversiGameSystem.Server.Persistence.Model;

/// <summary>
/// Represents persistence db configuration.
/// </summary>
public sealed class PersistenceDbConfiguration : DbConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceDbConfiguration"/> class.
    /// </summary>
    public PersistenceDbConfiguration()
    {
        SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
}

/// <summary>
/// Represents reversi game system entities.
/// </summary>
[DbConfigurationType(typeof(PersistenceDbConfiguration))]
public partial class ReversiGameSystemEntities
{
}
