using PibesDelDestino.Ratings;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

public interface IRatingAppService : IApplicationService
{
    Task<RatingDto> CreateAsync(CreateRatingDto input);
    Task<RatingDto> UpdateAsync(Guid id, CreateRatingDto input);
    Task<RatingDto> GetMyRatingAsync(Guid destinationId);
}