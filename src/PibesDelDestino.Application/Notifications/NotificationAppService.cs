using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Notifications
{
    [Authorize]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IRepository<AppNotification, Guid> _repository;

        public NotificationAppService(IRepository<AppNotification, Guid> repository)
        {
            _repository = repository;
        }

        // Este método obtiene todas las notificaciones del usuario actual
        public async Task<List<AppNotificationDto>> GetMyNotificationsAsync()

        {  // Obtenemos todas las notificaciones del usuario actual
            var notifications = await _repository.GetListAsync(x => x.UserId == CurrentUser.Id);
            return ObjectMapper.Map<List<AppNotification>, List<AppNotificationDto>>(
                notifications.OrderByDescending(x => x.CreationTime).ToList()
            );
        }

        // Este método marca una notificación específica como leída
        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _repository.GetAsync(id);
            if (notification.UserId != CurrentUser.Id) return;

            notification.IsRead = true;
            await _repository.UpdateAsync(notification);
        }

        // Este método marca una notificación específica como no leída
        public async Task MarkAsUnreadAsync(Guid id)
        {
            var notification = await _repository.GetAsync(id);
            if (notification.UserId != CurrentUser.Id) return;

            notification.IsRead = false;
            await _repository.UpdateAsync(notification);
        }

        // Este método obtiene el conteo de notificaciones no leídas del usuario actual
        public async Task<int> GetUnreadCountAsync()
        {
            return await _repository.CountAsync(x => x.UserId == CurrentUser.Id && !x.IsRead);
        }

        // Este método marca todas las notificaciones del usuario actual como leídas
        public async Task MarkAllAsReadAsync()
        {
            var userId = CurrentUser.Id.Value;

            // Buscamos todas las que NO están leídas de este usuario
            var unreadNotifications = await _repository.GetListAsync(n => n.UserId == userId && !n.IsRead);

            // Las recorremos y marcamos como leídas
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                await _repository.UpdateAsync(notification);
            }
        }
    }
}
