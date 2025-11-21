using Volo.Abp.Modularity;

namespace PibesDelDestino;

[DependsOn(
    typeof(PibesDelDestinoDomainModule),
    typeof(PibesDelDestinoTestBaseModule)
)]
public class PibesDelDestinoDomainTestModule : AbpModule
{

}
