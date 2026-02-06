using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using Volo.Abp.Identity;
using Volo.Abp.Data;

namespace PibesDelDestino.Notifications
{
    [Authorize]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IRepository<AppNotification, Guid> _repository;
        private readonly IIdentityUserRepository _userRepository;

        public NotificationAppService(IRepository<AppNotification, Guid> repository, IIdentityUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
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

        public async Task SetNotificationPreferenceAsync(string preference)
        {
            var user = await _userRepository.GetAsync(CurrentUser.Id.Value);

            // Valores esperados: "Mail", "Pantalla", "Ambas"
            user.SetProperty("NotifPref", preference);

            await _userRepository.UpdateAsync(user);
        }

        public async Task<string> GetNotificationPreferenceAsync()
        {
            var user = await _userRepository.GetAsync(CurrentUser.Id.Value);

            return user.GetProperty<string>("NotifPref") ?? "Ambas"; // Default "Ambas"
        }
    }
}
