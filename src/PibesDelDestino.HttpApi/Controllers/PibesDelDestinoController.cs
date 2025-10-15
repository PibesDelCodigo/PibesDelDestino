using PibesDelDestino.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace PibesDelDestino.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class PibesDelDestinoController : AbpControllerBase
{
    protected PibesDelDestinoController()
    {
        LocalizationResource = typeof(PibesDelDestinoResource);
    }
}
