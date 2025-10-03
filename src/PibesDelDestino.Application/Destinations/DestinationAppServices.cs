using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService :
        CrudAppService<
        Destination,
        DestinationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateDestinationDto>,
    IDestinationAppService
    {
        public DestinationAppService(IRepository<Destination, Guid> repository)
            : base(repository)
        {

        }

    }
}