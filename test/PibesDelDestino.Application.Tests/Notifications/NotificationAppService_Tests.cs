using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace PibesDelDestino.Notifications
{
    public abstract class NotificationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly INotificationAppService _notificationAppService;
        protected readonly IRepository<AppNotification, Guid> _notificationRepository;
        protected readonly ICurrentUser _currentUser;

        protected NotificationAppService_Tests()
        {
            // Resolvemos los servicios reales para tests de integración
            _notificationAppService = GetRequiredService<INotificationAppService>();
            _notificationRepository = GetRequiredService<IRepository<AppNotification, Guid>>();
            _currentUser = GetRequiredService<ICurrentUser>();
        }

        [Fact]
        public async Task GetMyNotificationsAsync_Should_Return_Only_User_Notifications()
        {
            // Arrange
            var myId = _currentUser.Id ?? Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            // Insertamos notificaciones usando tu constructor de 5 parámetros
            await _notificationRepository.InsertAsync(new AppNotification(Guid.NewGuid(), myId, "Título 1", "Mi mensaje", "Update"), autoSave: true);
            await _notificationRepository.InsertAsync(new AppNotification(Guid.NewGuid(), otherUserId, "Título 2", "Mensaje ajeno", "Update"), autoSave: true);

            // Act
            var result = await _notificationAppService.GetMyNotificationsAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].Message.ShouldBe("Mi mensaje");
        }

        [Fact]
        public async Task MarkAsReadAsync_Should_Update_Status_Only_If_Owner()
        {
            // Arrange
            var myId = _currentUser.Id ?? Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            await _notificationRepository.InsertAsync(
                new AppNotification(notificationId, myId, "Test", "Msg", "Info"),
                autoSave: true
            );

            // Act
            await _notificationAppService.MarkAsReadAsync(notificationId);

            // Assert
            var updated = await _notificationRepository.GetAsync(notificationId);
            updated.IsRead.ShouldBeTrue();
        }

        [Fact]
        public async Task MarkAllAsReadAsync_Should_Update_All_Unread_Notifications()
        {
            // Arrange
            var myId = _currentUser.Id ?? Guid.NewGuid();

            // Creamos 3 notificaciones no leídas (IsRead es false por defecto)
            for (int i = 0; i < 3; i++)
            {
                await _notificationRepository.InsertAsync(
                    new AppNotification(Guid.NewGuid(), myId, $"T{i}", $"M{i}", "Type"),
                    autoSave: true
                );
            }

            // Act
            await _notificationAppService.MarkAllAsReadAsync();

            // Assert
            var result = await _notificationAppService.GetMyNotificationsAsync();
            result.All(x => x.IsRead).ShouldBeTrue();

            var unreadCount = await _notificationAppService.GetUnreadCountAsync();
            unreadCount.ShouldBe(0);
        }

        [Fact]
        public async Task GetUnreadCountAsync_Should_Return_Correct_Count()
        {
            // Arrange
            var myId = _currentUser.Id ?? Guid.NewGuid();

            await _notificationRepository.InsertAsync(new AppNotification(Guid.NewGuid(), myId, "T1", "M1", "Type"), autoSave: true);
            await _notificationRepository.InsertAsync(new AppNotification(Guid.NewGuid(), myId, "T2", "M2", "Type"), autoSave: true);

            // Act
            var count = await _notificationAppService.GetUnreadCountAsync();

            // Assert
            count.ShouldBe(2);
        }
    }
}