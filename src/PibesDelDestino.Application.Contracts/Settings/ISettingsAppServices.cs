using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Settings
{
    public interface ISettingsAppService : IApplicationService
    {
        Task<bool> GetNotificationPreferenceAsync();
        Task UpdateNotificationPreferenceAsync(bool enabled);
    }
}