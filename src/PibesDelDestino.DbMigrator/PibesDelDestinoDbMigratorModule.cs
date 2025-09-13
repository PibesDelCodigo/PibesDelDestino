using PibesDelDestino.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace PibesDelDestino.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PibesDelDestinoEntityFrameworkCoreModule),
    typeof(PibesDelDestinoApplicationContractsModule)
)]
public class PibesDelDestinoDbMigratorModule : AbpModule
{
}
