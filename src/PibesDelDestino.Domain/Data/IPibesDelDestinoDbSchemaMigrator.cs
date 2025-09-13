using System.Threading.Tasks;

namespace PibesDelDestino.Data;

public interface IPibesDelDestinoDbSchemaMigrator
{
    Task MigrateAsync();
}
