using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace PibesDelDestino;

[DependsOn(
    typeof(PibesDelDestinoApplicationModule),
    typeof(PibesDelDestinoDomainTestModule)
)]
public class PibesDelDestinoApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Agrega esta línea para registrar el HttpClientFactory en el entorno de pruebas
        context.Services.AddHttpClient();
    }
}
