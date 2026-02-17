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
        Task MarkAsUnreadAsync(Guid id); 
        Task<int> GetUnreadCountAsync();
    }
}