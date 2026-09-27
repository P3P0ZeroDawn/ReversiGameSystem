using System.Data.Entity;
using System.Data.Entity.SqlServer;

namespace ReversiGameSystem.Server.Persistence.Model;

public sealed class PersistenceDbConfiguration : DbConfiguration
{
    public PersistenceDbConfiguration()
    {
        SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
    }
}

[DbConfigurationType(typeof(PersistenceDbConfiguration))]
public partial class ReversiGameSystemEntities
{
}
