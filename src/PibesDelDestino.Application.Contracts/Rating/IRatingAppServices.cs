using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Ratings
{
    public interface IRatingAppService : IApplicationService
    {
        Task<RatingDto> CreateAsync(CreateRatingDto input);

    }
}