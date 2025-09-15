using Volo.Abp.Modularity;

namespace PibesDelDestino;

[DependsOn(
    typeof(PibesDelDestinoApplicationModule),
    typeof(PibesDelDestinoDomainTestModule)
)]
public class PibesDelDestinoApplicationTestModule : AbpModule
{

}
