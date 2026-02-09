using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace PibesDelDestino.Users
{
    [Authorize] // Requiere estar logueado
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
        // REQUERIMIENTO 1.6: Backend para Perfil Público.
        // Este método expone los datos NO sensibles (nombre, foto) de un usuario
        // para que otros puedan verlo.
        public async Task<PublicUserDto> GetPublicProfileAsync(Guid userId)
        {
            // Buscamos al usuario por ID
            var user = await _userRepository.GetAsync(userId);

            // Mapeamos manual (o podrías usar ObjectMapper si configuras el perfil)
            return new PublicUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname
            };
        }
        // REQUERIMIENTO 1.5: Eliminar mi propia cuenta.
        public async Task DeleteSelfAsync()
        {
            var currentUserId = CurrentUser.Id;

            if (currentUserId == null)
            {
                throw new UserFriendlyException("No se pudo identificar al usuario actual.");
            }

            // Buscamos al usuario actual
            var user = await _userManager.FindByIdAsync(currentUserId.Value.ToString());

            if (user == null)
            {
                throw new UserFriendlyException("Usuario no encontrado.");
            }

            // ¡Acción crítica! Borramos al usuario.
            (await _userManager.DeleteAsync(user)).CheckErrors();
        }
    }
}