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
            // 1. Usamos el UserManager REAL
            _realUserManager = GetRequiredService<IdentityUserManager>();

            // 2. Mockeamos el CurrentUser
            _currentUserMock = Substitute.For<ICurrentUser>();

            // 3. Mockeamos el ServiceProvider
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(ICurrentUser)).Returns(_currentUserMock);

            // 4. Instanciamos el Proxy del Servicio
            _settingsAppService = new SettingsAppServiceTestProxy(
                _realUserManager,
                serviceProviderMock // Pasamos el mock para que el LazyServiceProvider funcione
            );
        }

        [Fact]
        public async Task Should_Update_Preferences()
        {
            // ARRANGE
            var user = new IdentityUser(Guid.NewGuid(), "testuser", "test@test.com");
            await _realUserManager.CreateAsync(user);

            _currentUserMock.Id.Returns(user.Id);

            // Preparamos el DTO (como lo espera tu servicio real)
            var input = new UserPreferencesDto
            {
                ReceiveNotifications = false,
                NotificationType = 1 // Solo Email
            };

            // ACT
            // Llamamos al método real que tenés en tu servicio
            await _settingsAppService.UpdatePreferencesAsync(input);

            // ASSERT
            // Verificamos en la base de datos real
            var updatedUser = await _realUserManager.GetByIdAsync(user.Id);

            // Verificamos que se hayan guardado las dos propiedades
            updatedUser.GetProperty<bool>("ReceiveNotifications").ShouldBe(false);
            updatedUser.GetProperty<int>("NotificationType").ShouldBe(1);
        }

        [Fact]
        public async Task Should_Get_Preferences_Default()
        {
            // ARRANGE
            var user = new IdentityUser(Guid.NewGuid(), "testuser2", "test2@test.com");
            // No seteamos nada, así probamos los defaults (True y 2)
            await _realUserManager.CreateAsync(user);

            _currentUserMock.Id.Returns(user.Id);

            // ACT
            var result = await _settingsAppService.GetPreferencesAsync();

            // ASSERT
            result.ReceiveNotifications.ShouldBe(true); // Default
            result.NotificationType.ShouldBe(2);        // Default
        }
    }

    // --- PROXY ---
    public class SettingsAppServiceTestProxy : SettingsAppService
    {
        // Ajustamos el constructor del Proxy para que coincida con el tuyo
        public SettingsAppServiceTestProxy(
            IdentityUserManager userManager,
            IServiceProvider serviceProvider)
            : base(userManager)
        {
            LazyServiceProvider = new AbpLazyServiceProvider(serviceProvider);
        }
    }
}