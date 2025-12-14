using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Favorites
{
    public interface IFavoriteAppService : IApplicationService
    {
        Task<bool> ToggleAsync(CreateFavoriteDto input);
        Task<bool> IsFavoriteAsync(CreateFavoriteDto input); // Para saber de qué color pintar el corazón
    }
}