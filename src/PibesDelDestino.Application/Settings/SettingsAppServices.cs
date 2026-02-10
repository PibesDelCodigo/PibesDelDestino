using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace PibesDelDestino.Settings
{
    // 1. DTO para transportar los datos (Lo ponemos aquí para simplificar)
    public class UserPreferencesDto
    {
        public bool ReceiveNotifications { get; set; }
        public int NotificationType { get; set; } // 0: Pantalla, 1: Email, 2: Ambos
    }

    [Authorize]
    public class SettingsAppService : ApplicationService
    {
        private readonly IdentityUserManager _userManager;

        public SettingsAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        // 1. Obtener TODAS las preferencias
        public async Task<UserPreferencesDto> GetPreferencesAsync()
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            return new UserPreferencesDto
            {
                // Si es null, asumimos True (Activado)
                ReceiveNotifications = user.GetProperty<bool?>("ReceiveNotifications") ?? true,

                // Si es null, asumimos 2 (Ambos) por defecto
                NotificationType = user.GetProperty<int?>("NotificationType") ?? 2
            };
        }

        // 2. Guardar TODOS los cambios
        public async Task UpdatePreferencesAsync(UserPreferencesDto input)
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            // Guardamos el ON/OFF
            user.SetProperty("ReceiveNotifications", input.ReceiveNotifications);

            // Guardamos el TIPO (0, 1 o 2)
            user.SetProperty("NotificationType", input.NotificationType);

            // Guardamos en Base de Datos (Esto es seguro, no borra el UserName)
            await _userManager.UpdateAsync(user);
        }
    }
}