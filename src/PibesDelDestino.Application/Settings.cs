using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace PibesDelDestino.Settings
{
    [Authorize]
    public class SettingsAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;

        public SettingsAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        // 1. Obtener estado actual
        public async Task<bool> GetNotificationPreferenceAsync()
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            // Buscamos la propiedad. Si no existe, devolvemos 'true' por defecto.
            return user.GetProperty<bool?>("ReceiveNotifications") ?? true;
        }

        // 2. Guardar cambio (Activar/Desactivar)
        public async Task UpdateNotificationPreferenceAsync(bool enabled)
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            // Guardamos el valor en las propiedades extra del usuario
            user.SetProperty("ReceiveNotifications", enabled);

            await _userManager.UpdateAsync(user);
        }
    }
}