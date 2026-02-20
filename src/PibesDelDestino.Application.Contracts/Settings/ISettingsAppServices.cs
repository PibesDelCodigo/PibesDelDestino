using System.Threading.Tasks;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Settings
{
    public interface ISettingsAppService : IApplicationService
    {
        Task<UserPreferencesDto> GetPreferencesAsync();

        Task UpdatePreferencesAsync(UserPreferencesDto input);
    }
}