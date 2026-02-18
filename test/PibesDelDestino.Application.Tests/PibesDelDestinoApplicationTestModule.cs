using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using NSubstitute; 
using PibesDelDestino.Notifications; 
using PibesDelDestino.Cities; 

namespace PibesDelDestino;

[DependsOn(
    typeof(PibesDelDestinoApplicationModule),
    typeof(PibesDelDestinoDomainTestModule)
)]
public class PibesDelDestinoApplicationTestModule : AbpModule
{

}