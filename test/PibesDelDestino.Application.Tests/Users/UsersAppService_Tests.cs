using Microsoft.AspNetCore.Identity;
using Shouldly;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PibesDelDestino.Users
{
    public abstract class AppUserAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
    {
  
        private readonly IAppUserAppService _appUserAppService;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IdentityUserManager _userManager;
        private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

        public AppUserAppService_Tests()
        {
            _appUserAppService = GetRequiredService<IAppUserAppService>();
            _userRepository = GetRequiredService<IIdentityUserRepository>();
            _userManager = GetRequiredService<IdentityUserManager>();
            _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        }

        [Fact]
        public async Task Should_Get_Public_Profile()
        {
            // Arrange: Creamos un usuario de prueba para sacarle el Guid
            var userId = Guid.NewGuid();
            var tempUser = new IdentityUser(userId, "testpublic", "public@test.com");
            (await _userManager.CreateAsync(tempUser, "123456Aa!")).CheckErrors();

            // Act: Usamos tu método pasándole el Guid
            var publicProfile = await _appUserAppService.GetPublicProfileAsync(userId);

            // Assert
            publicProfile.ShouldNotBeNull();
            publicProfile.UserName.ShouldBe("testpublic");
            publicProfile.Id.ShouldBe(userId);
        }

        [Fact]
        public async Task Should_Delete_Account()
        {
            // Arrange: Creamos el usuario a borrar
            var userId = Guid.NewGuid();
            var tempUser = new IdentityUser(userId, "userborrar", "userborrar@test.com");
            (await _userManager.CreateAsync(tempUser, "123456Aa!")).CheckErrors();

            // Act: Nos disfrazamos del usuario que queremos borrar
            var claims = new[] { new Claim(AbpClaimTypes.UserId, tempUser.Id.ToString()) };

            using (_currentPrincipalAccessor.Change(new ClaimsPrincipal(new ClaimsIdentity(claims))))
            {
                // Llamamos a TU método exacto
                await _appUserAppService.DeleteSelfAsync();
            }

            // Assert: Revisamos que ya no exista en la DB
            var deletedUser = await _userRepository.FindAsync(userId);
            deletedUser.ShouldBeNull();
        }
    }
}