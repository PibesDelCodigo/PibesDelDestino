using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Ratings
{
    // Hereda de IApplicationService (la interfaz base de ABP para servicios)
    public interface IRatingAppService : IApplicationService
    {
        Task<RatingDto> CreateAsync(CreateRatingDto input);

        // En el futuro, podríamos agregar más métodos aquí, como:
        // Task<List<RatingDto>> GetMyRatingsAsync();
    }
}