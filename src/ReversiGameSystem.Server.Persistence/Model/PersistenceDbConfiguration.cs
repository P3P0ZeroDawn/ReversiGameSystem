using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Linq;

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
    /// <summary>
    /// Initializes Entity Framework metadata, runtime mapping views, and the login query plan
    /// before client requests arrive.
    /// </summary>
    public static void WarmUp()
    {
        using (ReversiGameSystemEntities context = new ReversiGameSystemEntities())
        {
            ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace
                .LoadFromAssembly(typeof(Account).Assembly);
            _ = context.Account
                .Include(account => account.AccountSettings)
                .AsNoTracking()
                .FirstOrDefault(account =>
                    account.username == string.Empty && account.emailAddress == string.Empty);
        }
    }
}
