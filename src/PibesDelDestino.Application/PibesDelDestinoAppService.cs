using PibesDelDestino.Localization;
using Volo.Abp.Application.Services;

namespace PibesDelDestino;

/* Inherit your application services from this class.
 */
public abstract class PibesDelDestinoAppService : ApplicationService
{
    protected PibesDelDestinoAppService()
    {
        LocalizationResource = typeof(PibesDelDestinoResource);
    }
}
