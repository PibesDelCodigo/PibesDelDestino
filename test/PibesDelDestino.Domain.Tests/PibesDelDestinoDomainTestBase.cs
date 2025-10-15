using Volo.Abp.Modularity;

namespace PibesDelDestino;

/* Inherit from this class for your domain layer tests. */
public abstract class PibesDelDestinoDomainTestBase<TStartupModule> : PibesDelDestinoTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
