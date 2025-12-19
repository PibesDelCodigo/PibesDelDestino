using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using PibesDelDestino.Notifications;
using Volo.Abp.Users;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace PibesDelDestino.Notifications
{
    // ✅ LA CLASE DE TEST DEBE SER PÚBLICA
    public class NotificationAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly INotificationAppService _notificationAppService;
        private readonly IRepository<AppNotification, Guid> _notificationRepositoryMock;

        public NotificationAppService_Tests()
        {
            _notificationRepositoryMock = Substitute.For<IRepository<AppNotification, Guid>>();

            // Usamos la clase proxy pública definida abajo
            _notificationAppService = new NotificationAppServiceTestProxy(
                _notificationRepositoryMock,
                ServiceProvider // Usamos el ServiceProvider heredado de la clase base
            );
        }

        [Fact]
        public async Task Should_Get_Unread_Count_Successfully()
        {
            // ARRANGE
            _notificationRepositoryMock.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AppNotification, bool>>>())
                .Returns(Task.FromResult(10));

            // ACT
            var count = await _notificationAppService.GetUnreadCountAsync();

            // ASSERT
            count.ShouldBe(10);
        }
    }

    // ✅ LA CLASE PROXY DEBE SER PÚBLICA Y ESTAR FUERA O SER MIEMBRO PÚBLICO
    public class NotificationAppServiceTestProxy : NotificationAppService
    {
        public NotificationAppServiceTestProxy(
            IRepository<AppNotification, Guid> repository,
            IServiceProvider serviceProvider) : base(repository)
        {
            // Resolvemos el LazyServiceProvider para que CurrentUser y ObjectMapper funcionen
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}