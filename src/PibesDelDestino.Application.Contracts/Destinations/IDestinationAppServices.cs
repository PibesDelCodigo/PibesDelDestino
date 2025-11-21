using PibesDelDestino.Application.Contracts.Destinations;
using System;
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

    }
}