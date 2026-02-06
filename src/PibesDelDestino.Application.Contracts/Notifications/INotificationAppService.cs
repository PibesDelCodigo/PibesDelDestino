using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Notifications
{
    public interface INotificationAppService : IApplicationService
    {
        Task<List<AppNotificationDto>> GetMyNotificationsAsync();
        Task MarkAsReadAsync(Guid id);
        Task MarkAsUnreadAsync(Guid id); // Para cumplir el 6.4 completo (toggle)
        Task<int> GetUnreadCountAsync();
        Task SetNotificationPreferenceAsync(string preference);
        Task<string> GetNotificationPreferenceAsync();
    }
}