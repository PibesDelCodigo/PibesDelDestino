using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using PibesDelDestino.Workers;
using Volo.Abp.Emailing;

namespace PibesDelDestino;

[DependsOn(
    typeof(PibesDelDestinoDomainModule),
    typeof(PibesDelDestinoApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpEmailingModule)
    )]
public class PibesDelDestinoApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClient();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PibesDelDestinoApplicationModule>();
        });
    }
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<EventCheckingWorker>();
    }
}
