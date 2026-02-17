using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Users
{
    public interface IAppUserAppService : IApplicationService
    {
        Task<PublicUserDto> GetPublicProfileAsync(Guid userId);

        Task DeleteSelfAsync();
    }
}