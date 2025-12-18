using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;


namespace PibesDelDestino.Destinations
{
    public interface IDestinationAppService :
        ICrudAppService<
        DestinationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateDestinationDto>
    {
        Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request);
        Task<List<DestinationDto>> GetTopDestinationsAsync();
    }
}