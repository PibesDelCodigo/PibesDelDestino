using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using Xunit;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Data;
using Microsoft.Extensions.DependencyInjection;

namespace PibesDelDestino.Settings
{
    public class SettingsAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly SettingsAppService _settingsAppService;
        private readonly IdentityUserManager _realUserManager;
        private readonly ICurrentUser _currentUserMock;

        public SettingsAppService_Tests()
        {
            _realUserManager = GetRequiredService<IdentityUserManager>();
            _currentUserMock = Substitute.For<ICurrentUser>();
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(ICurrentUser)).Returns(_currentUserMock);

            _settingsAppService = new SettingsAppServiceTestProxy(
                _realUserManager,
                serviceProviderMock
            );
        }

        [Fact]
        public async Task Should_Update_Preferences()
        {
            var user = new IdentityUser(Guid.NewGuid(), "testuser", "test@test.com");
            await _realUserManager.CreateAsync(user);

            _currentUserMock.Id.Returns(user.Id);
            var input = new UserPreferencesDto
            {
                ReceiveNotifications = false,
                NotificationType = 1 // Solo Email
            };
            await _settingsAppService.UpdatePreferencesAsync(input);

            // Verificamos en la base de datos real
            var updatedUser = await _realUserManager.GetByIdAsync(user.Id);

            // Verificamos que se hayan guardado las dos propiedades
            updatedUser.GetProperty<bool>("ReceiveNotifications").ShouldBe(false);
            updatedUser.GetProperty<int>("NotificationType").ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Preferences_Default()
        {

            var user = new IdentityUser(Guid.NewGuid(), "testuser2", "test2@test.com");
            // No seteamos nada, así probamos los defaults
            await _realUserManager.CreateAsync(user);

            _currentUserMock.Id.Returns(user.Id);

            var result = await _settingsAppService.GetPreferencesAsync();

            result.ReceiveNotifications.ShouldBe(true);
            result.NotificationType.ShouldBe(2); 
        }
    }

    public class SettingsAppServiceTestProxy : SettingsAppService
    {
        public SettingsAppServiceTestProxy(
            IdentityUserManager userManager,
            IServiceProvider serviceProvider)
            : base(userManager)
        {
            LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
        }
    }
}