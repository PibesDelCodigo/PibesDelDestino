using System.ComponentModel.DataAnnotations;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace PibesDelDestino;

public static class PibesDelDestinoModuleExtensionConfigurator
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        OneTimeRunner.Run(() =>
        {
            ConfigureExistingProperties();
            ConfigureExtraProperties();
        });
    }

    private static void ConfigureExistingProperties()
    {
        /* You can change max lengths for properties of the
         * entities defined in the modules used by your application.
         * ... (comentarios originales) ...
         */
    }

    private static void ConfigureExtraProperties()
    {
        // ✅ ACÁ AGREGAMOS LA LÓGICA DE FORMA SEGURA
        // El OneTimeRunner protege esto para que los tests no choquen entre sí.

        ObjectExtensionManager.Instance.Modules()
            .ConfigureIdentity(identity =>
            {
                identity.ConfigureUser(user =>
                {
                    // Agregamos la propiedad extra al usuario
                    user.AddOrUpdateProperty<string>("ProfilePictureUrl");
                });
            });
    }
}