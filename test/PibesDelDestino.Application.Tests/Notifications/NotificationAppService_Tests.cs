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
    public class NotificationAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly NotificationAppService _notificationAppService;
        private readonly IRepository<AppNotification, Guid> _notificationRepositoryMock;

        public NotificationAppService_Tests()
        {
            _notificationRepositoryMock = Substitute.For<IRepository<AppNotification, Guid>>();

            _notificationAppService = new NotificationAppServiceTestProxy(
                _notificationRepositoryMock,
                ServiceProvider
            );
        }

        [Fact]
        public async Task Should_Get_Unread_Count_Successfully()
        {
            _notificationRepositoryMock.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<AppNotification, bool>>>())
                .Returns(Task.FromResult(10));

            var count = await _notificationAppService.GetUnreadCountAsync();

            count.ShouldBe(10);
        }
    }

    public class NotificationAppServiceTestProxy : NotificationAppService
    {
        public NotificationAppServiceTestProxy(
            IRepository<AppNotification, Guid> repository,
            IServiceProvider serviceProvider) : base(repository)
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}