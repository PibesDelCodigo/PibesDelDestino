using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Experiences
{
    public interface ITravelExperienceAppService : ICrudAppService<
        TravelExperienceDto,
        Guid,
        GetTravelExperiencesInput,
        CreateUpdateTravelExperienceDto>
    {
    
        Task<double> GetAverageRatingAsync(Guid destinationId);
    }
}