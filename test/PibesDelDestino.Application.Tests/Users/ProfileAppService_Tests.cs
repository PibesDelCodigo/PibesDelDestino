using Microsoft.AspNetCore.Identity;
using Shouldly;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Volo.Abp.Account;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace PibesDelDestino.Users
{
    public abstract class ProfileAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IProfileAppService _profileAppService;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IdentityUserManager _userManager;
        private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

        public ProfileAppService_Tests()
        {
            _profileAppService = GetRequiredService<IProfileAppService>();
            _userRepository = GetRequiredService<IIdentityUserRepository>();
            _userManager = GetRequiredService<IdentityUserManager>();
            _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        }

        [Fact]
        public async Task Should_Update_My_Profile_Extra_Properties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new IdentityUser(userId, "testuser", "testuser@abp.io");
            (await _userManager.CreateAsync(user, "123456Aa!")).CheckErrors();

            var newPic = "https://pibesdeldestino.com/mifoto.png";
            var claims = new[] { new Claim(AbpClaimTypes.UserId, userId.ToString()) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act: Nos disfrazamos del usuario para editar su perfil
            using (_currentPrincipalAccessor.Change(principal))
            {
                var currentProfileDto = await _profileAppService.GetAsync();

                var input = new UpdateProfileDto
                {
                    UserName = currentProfileDto.UserName,
                    Email = currentProfileDto.Email,
                    Name = "NombreUpdated",
                    Surname = "ApellidoUpdated",
                    PhoneNumber = "999888777",
                };

                // Agregamos LA ÚNICA extra property que configuraste vos en tu proyecto
                input.ExtraProperties["ProfilePictureUrl"] = newPic;
                await _profileAppService.UpdateAsync(input);
            }

            // Assert
            var userInDb = await _userRepository.GetAsync(userId);
            userInDb.Name.ShouldBe("NombreUpdated");
            userInDb.Surname.ShouldBe("ApellidoUpdated");

            // Comprobamos que la foto se guardó usando tu extensión de ABP
            userInDb.GetProperty<string>("ProfilePictureUrl").ShouldBe(newPic);
        }
    }
}