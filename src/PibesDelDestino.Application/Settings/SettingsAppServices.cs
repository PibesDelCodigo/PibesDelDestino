using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Data;

namespace PibesDelDestino.Settings
{
    [Authorize]
    public class SettingsAppService : ApplicationService, ISettingsAppService
    {
        private readonly IdentityUserManager _userManager;

        private const string KeyReceive = "ReceiveNotifications";
        // Cambiamos la constante para ser más claros (opcional, pero recomendado)
        private const string KeyChannel = "PreferredChannel";

        public SettingsAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserPreferencesDto> GetPreferencesAsync()
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            // Leemos como int y casteamos a nuestro nuevo Enum
            var channelValue = user.GetProperty<int?>(KeyChannel) ?? (int)NotificationChannel.All;

            return new UserPreferencesDto
            {
                ReceiveNotifications = user.GetProperty<bool?>(KeyReceive) ?? true,
                PreferredChannel = (NotificationChannel)channelValue
            };
        }

        public async Task UpdatePreferencesAsync(UserPreferencesDto input)
        {
            var user = await _userManager.GetByIdAsync(CurrentUser.Id.Value);

            user.SetProperty(KeyReceive, input.ReceiveNotifications);

            // Guardamos el canal preferido
            user.SetProperty(KeyChannel, (int)input.PreferredChannel);

            await _userManager.UpdateAsync(user);
        }
    }
}