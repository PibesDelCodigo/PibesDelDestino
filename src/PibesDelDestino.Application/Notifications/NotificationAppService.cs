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

        public async Task<List<AppNotificationDto>> GetMyNotificationsAsync()
        {
            var notifications = await _repository.GetListAsync(x => x.UserId == CurrentUser.Id);
            return ObjectMapper.Map<List<AppNotification>, List<AppNotificationDto>>(
                notifications.OrderByDescending(x => x.CreationTime).ToList()
            );
        }

        // CUMPLE 6.4: Marcar como leída
        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _repository.GetAsync(id);
            if (notification.UserId != CurrentUser.Id) return;

            notification.IsRead = true;
            await _repository.UpdateAsync(notification);
        }

        // CUMPLE 6.4: Marcar como NO leída
        public async Task MarkAsUnreadAsync(Guid id)
        {
            var notification = await _repository.GetAsync(id);
            if (notification.UserId != CurrentUser.Id) return;

            notification.IsRead = false;
            await _repository.UpdateAsync(notification);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _repository.CountAsync(x => x.UserId == CurrentUser.Id && !x.IsRead);
        }

        public async Task MarkAllAsReadAsync()
        {
            var userId = CurrentUser.Id.Value;

            // 1. Buscamos todas las que NO están leídas de este usuario
            var unreadNotifications = await _repository.GetListAsync(n => n.UserId == userId && !n.IsRead);

            // 2. Las recorremos y marcamos como leídas
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                // No hace falta llamar a UpdateAsync explícitamente si usas UnitOfWork (ABP lo hace solo al final),
                // pero si quieres asegurar, puedes descomentar la siguiente línea:
                await _repository.UpdateAsync(notification);
            }
        }
    }
}
