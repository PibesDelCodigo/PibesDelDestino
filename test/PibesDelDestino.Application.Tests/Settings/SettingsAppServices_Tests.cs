using Shouldly;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;

namespace PibesDelDestino.Settings
{
    public abstract class SettingsAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly ISettingsAppService _settingsAppService;
        protected readonly IdentityUserManager _userManager;
        protected readonly ICurrentUser _currentUser;

        protected SettingsAppService_Tests()
        {
            _settingsAppService = GetRequiredService<ISettingsAppService>();
            _userManager = GetRequiredService<IdentityUserManager>();
            _currentUser = GetRequiredService<ICurrentUser>();
        }

        [Fact]
        public async Task GetPreferencesAsync_Should_Return_Default_Values_For_New_User()
        {
            // --- ARRANGE ---
            // Obtenemos el ID que ABP le asignó al "usuario actual" en el entorno de test
            var currentUserId = _currentUser.Id.Value;

            // Creamos físicamente al usuario en la DB de memoria
            await WithUnitOfWorkAsync(async () => {
                var user = new IdentityUser(currentUserId, "admin", "admin@pibes.com");
                await _userManager.CreateAsync(user);
            });

            // --- ACT ---
            var result = await _settingsAppService.GetPreferencesAsync();

            // --- ASSERT ---
            result.ReceiveNotifications.ShouldBeTrue();
            result.PreferredChannel.ShouldBe(NotificationChannel.All);
        }

        [Fact]
        public async Task UpdatePreferencesAsync_Should_Persist_Changes()
        {
            // --- ARRANGE ---
            var currentUserId = _currentUser.Id.Value;
            await WithUnitOfWorkAsync(async () => {
                await _userManager.CreateAsync(new IdentityUser(currentUserId, "admin", "admin@pibes.com"));
            });

            var input = new UserPreferencesDto
            {
                ReceiveNotifications = false,
                PreferredChannel = NotificationChannel.Push
            };

            // --- ACT ---
            await _settingsAppService.UpdatePreferencesAsync(input);
            var updatedResult = await _settingsAppService.GetPreferencesAsync();

            // --- ASSERT ---
            updatedResult.ReceiveNotifications.ShouldBeFalse();
            updatedResult.PreferredChannel.ShouldBe(NotificationChannel.Push);
        }

        // --- AGREGÁ ESTE MÉTODO AL FINAL DE LA CLASE ---
        protected async Task CreateTestUserAsync()
        {
            var currentUserId = _currentUser.Id.Value;
            await WithUnitOfWorkAsync(async () => {
                // Creamos al usuario con el ID que espera el entorno de pruebas
                var user = new IdentityUser(currentUserId, "testuser", "test@pibes.com");
                var result = await _userManager.CreateAsync(user);
                result.Succeeded.ShouldBeTrue(); // Verificamos que se creó bien
            });
        }

        // --- ASÍ QUEDARÍA TU TERCER TEST AHORA ---
        [Fact]
        public async Task UpdatePreferencesAsync_Should_Save_Directly_In_IdentityUser()
        {
            // Arrange
            await CreateTestUserAsync(); // <--- Llamamos al helper aquí

            var input = new UserPreferencesDto
            {
                ReceiveNotifications = false,
                PreferredChannel = NotificationChannel.Email
            };

            // Act
            await _settingsAppService.UpdatePreferencesAsync(input);

            // Assert
            // Validamos que en la DB de Identity el usuario tenga las propiedades grabadas
            var user = await _userManager.GetByIdAsync(_currentUser.Id.Value);

            user.GetProperty<bool>("ReceiveNotifications").ShouldBeFalse();
            user.GetProperty<int>("PreferredChannel").ShouldBe((int)NotificationChannel.Email);
        }
    }
}