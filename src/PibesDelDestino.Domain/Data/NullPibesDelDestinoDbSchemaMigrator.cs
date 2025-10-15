using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PibesDelDestino.Data;

/* This is used if database provider does't define
 * IPibesDelDestinoDbSchemaMigrator implementation.
 */
public class NullPibesDelDestinoDbSchemaMigrator : IPibesDelDestinoDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
