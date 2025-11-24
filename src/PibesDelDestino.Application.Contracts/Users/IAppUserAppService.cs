using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Users
{
    public interface IAppUserAppService : IApplicationService
    {
        // Punto 1.6: Ver perfil de otro
        Task<PublicUserDto> GetPublicProfileAsync(Guid userId);

        // Punto 1.5: Eliminar mi propia cuenta
        Task DeleteSelfAsync();
    }
}