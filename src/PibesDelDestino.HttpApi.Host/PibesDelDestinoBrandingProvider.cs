using Microsoft.Extensions.Localization;
using PibesDelDestino.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace PibesDelDestino;

[Dependency(ReplaceServices = true)]
public class PibesDelDestinoBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PibesDelDestinoResource> _localizer;

    public PibesDelDestinoBrandingProvider(IStringLocalizer<PibesDelDestinoResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
