using PibesDelDestino.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace PibesDelDestino.Permissions;

public class PibesDelDestinoPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(PibesDelDestinoPermissions.GroupName);

    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PibesDelDestinoResource>(name);
    }
}
