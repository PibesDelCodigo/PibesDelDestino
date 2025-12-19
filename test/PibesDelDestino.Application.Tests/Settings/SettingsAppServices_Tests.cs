using System;
using System.Threading.Tasks;
using System.Runtime.Serialization; // Vital para el truco de FormatterServices
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using Xunit;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace PibesDelDestino.Settings
{
    public class SettingsAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly SettingsAppService _settingsAppService;
        private readonly MockIdentityUserManager _userManagerManualMock;
        private readonly ICurrentUser _currentUserMock;

        public SettingsAppService_Tests()
        {
            // 1. Mock del CurrentUser
            _currentUserMock = Substitute.For<ICurrentUser>();
            _currentUserMock.Id.Returns(Guid.NewGuid());

            // 2. Mock del ServiceProvider
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(ICurrentUser)).Returns(_currentUserMock);

            // 3. LA SOLUCIÓN: Creamos el UserManager saltando el constructor
            // Esto evita que se ejecute el constructor de abajo que tiene nulls, 
            // evitando el ArgumentNullException del 'store'.
            _userManagerManualMock = (MockIdentityUserManager)FormatterServices.GetUninitializedObject(typeof(MockIdentityUserManager));

            // 4. Instancia del Proxy del Servicio
            _settingsAppService = new SettingsAppServiceTestProxy(
                _userManagerManualMock,
                serviceProviderMock
            );
        }

        [Fact]
        public async Task Should_Update_Preference()
        {
            // ARRANGE
            var userId = _currentUserMock.Id.Value;
            var fakeUser = new Volo.Abp.Identity.IdentityUser(userId, "testuser", "test@test.com");

            // Configuramos nuestro Mock manual para que devuelva el usuario
            _userManagerManualMock.UserToReturn = fakeUser;

            // ACT
            await _settingsAppService.UpdateNotificationPreferenceAsync(false);

            // ASSERT
            _userManagerManualMock.UpdateCalled.ShouldBeTrue();
            fakeUser.GetProperty<bool>("ReceiveNotifications").ShouldBe(false);
        }

        [Fact]
        public async Task Should_Get_Notification_Preference_Default_True()
        {
            // ARRANGE
            var userId = _currentUserMock.Id.Value;
            var fakeUser = new Volo.Abp.Identity.IdentityUser(userId, "testuser", "test@test.com");

            _userManagerManualMock.UserToReturn = fakeUser;

            // ACT
            var result = await _settingsAppService.GetNotificationPreferenceAsync();

            // ASSERT
            result.ShouldBe(true);
        }
    }

    // --- PROXY PARA INYECTAR LAZYSERVICEPROVIDER ---
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

    // --- MOCK MANUAL DEL USER MANAGER ---
    public class MockIdentityUserManager : IdentityUserManager
    {
        // Constructor "Dummy" solo para que compile.
        // NUNCA SE EJECUTA gracias a FormatterServices.GetUninitializedObject
        public MockIdentityUserManager()
            : base(
                null!, // store
                null!, // roleRepo
                null!, // userRepo
                null!, // options
                null!, // hasher
                null!, // userValidators
                null!, // passwordValidators
                null!, // keyNormalizer
                null!, // errors
                null!, // services
                null!, // logger
                null!, // cancellationToken
                null!, // orgUnitRepo
                null!, // settingProvider
                null!, // eventBus
                null!, // linkUserRepo
                null!  // claimCache
            )
        {
        }

        // Propiedades de control para el Test
        public Volo.Abp.Identity.IdentityUser UserToReturn { get; set; }
        public bool UpdateCalled { get; private set; }

        // Sobreescribimos GetByIdAsync
        public override Task<Volo.Abp.Identity.IdentityUser> GetByIdAsync(Guid id)
        {
            return Task.FromResult(UserToReturn);
        }

        // Sobreescribimos UpdateAsync
        public override Task<IdentityResult> UpdateAsync(Volo.Abp.Identity.IdentityUser user)
        {
            UpdateCalled = true;
            return Task.FromResult(IdentityResult.Success);
        }
    }
}