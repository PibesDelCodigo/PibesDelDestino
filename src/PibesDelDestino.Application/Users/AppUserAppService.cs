using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace PibesDelDestino.Users
{
    [Authorize]

    // Este servicio se encarga de manejar las operaciones relacionadas con los usuarios,
    // como obtener su perfil público y eliminar su cuenta.
    public class AppUserAppService : ApplicationService, IAppUserAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IIdentityUserRepository _userRepository;

        public AppUserAppService(
            IdentityUserManager userManager,
            IIdentityUserRepository userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
        }
        [AllowAnonymous]

        // Este método permite obtener el perfil público de un usuario a través de su ID.
        public async Task<PublicUserDto> GetPublicProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetAsync(userId);

            return new PublicUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                ProfilePictureUrl = user.GetProperty<string>("ProfilePictureUrl")
            };

        }

        // Este método permite a un usuario eliminar su propia cuenta.
        public async Task DeleteSelfAsync()
        {
            var currentUserId = CurrentUser.Id;

            if (currentUserId == null)
            {
                throw new UserFriendlyException("No se pudo identificar al usuario actual.");
            }

            // Buscamos el usuario en la base de datos utilizando su ID. Si no se encuentra, lanzamos una excepción.
            var user = await _userManager.FindByIdAsync(currentUserId.Value.ToString());

            if (user == null)
            {
                throw new UserFriendlyException("Usuario no encontrado.");
            }

            (await _userManager.DeleteAsync(user)).CheckErrors();
        }
    }
}