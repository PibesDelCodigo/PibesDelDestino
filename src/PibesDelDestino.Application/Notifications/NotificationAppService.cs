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
        {
            var query = await _repository.GetQueryableAsync();
            var myNotifications = query
                .Where(x => x.UserId == CurrentUser.Id)
                .OrderByDescending(x => x.CreationTime);
            var result = await AsyncExecuter.ToListAsync(myNotifications.Take(50));

            return ObjectMapper.Map<List<AppNotification>, List<AppNotificationDto>>(result);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _repository.CountAsync(x => x.UserId == CurrentUser.Id && !x.IsRead);
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

        // Este método marca todas las notificaciones del usuario actual como leídas
public async Task MarkAllAsReadAsync()
        {
            var userId = CurrentUser.Id.Value;

            // 1. Traemos solo las que no están leídas
            var unreadNotifications = await _repository.GetListAsync(n => n.UserId == userId && !n.IsRead);

            if (!unreadNotifications.Any()) return;

            // Modificamos los objetos en memoria.
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            // ABP y EF Core detectan los cambios en la lista y hacen un UPDATE optimizado con el UpdateMany.
            await _repository.UpdateManyAsync(unreadNotifications);
        }
    }
}
