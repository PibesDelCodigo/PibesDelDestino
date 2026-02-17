using PibesDelDestino.Application.Contracts.Destinations;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Favorites
{
    public interface IFavoriteAppService : IApplicationService
    {
        Task<bool> ToggleAsync(CreateFavoriteDto input);
        Task<List<DestinationDto>> GetMyFavoritesAsync();
        Task<bool> IsFavoriteAsync(CreateFavoriteDto input);
    }


}