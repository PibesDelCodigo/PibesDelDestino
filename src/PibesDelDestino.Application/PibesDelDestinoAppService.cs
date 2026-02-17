using PibesDelDestino.Localization;
using Volo.Abp.Application.Services;

namespace PibesDelDestino;
public abstract class PibesDelDestinoAppService : ApplicationService
{
    protected PibesDelDestinoAppService()
    {
        LocalizationResource = typeof(PibesDelDestinoResource);
    }
}
