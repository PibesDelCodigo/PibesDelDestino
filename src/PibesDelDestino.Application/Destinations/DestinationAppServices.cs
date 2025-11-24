using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Destinations;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;


namespace PibesDelDestino.Destinations
{
    public class DestinationAppService :
        CrudAppService<Destination, DestinationDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateDestinationDto>,
    IDestinationAppService
    {
        private readonly ICitySearchService _citySearchService;
        private readonly IGuidGenerator _guidGenerator;

        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _guidGenerator = guidGenerator;
        }

        public override async Task<DestinationDto> CreateAsync(CreateUpdateDestinationDto input)
        {
            var destination = new Destination(
                _guidGenerator.Create(),
                input.Name,
                input.Country,
                input.City,
                input.Population,
                input.Photo,
                input.UpdateDate,
                new Coordinates(input.Coordinates.Latitude, input.Coordinates.Longitude)
            );

            await Repository.InsertAsync(destination);

            // El problema estaba probablemente aquí, en el mapeo de vuelta al DTO.
            // Esta es la versión correcta.
            return new DestinationDto
            {
                Id = destination.Id, // <-- ESTA LÍNEA ES LA CLAVE
                Name = destination.Name,
                Country = destination.Country,
                City = destination.City,
                Population = destination.Population,
                Photo = destination.Photo,
                UpdateDate = destination.UpdateDate,
                Coordinates = new CoordinatesDto
                {
                    Latitude = destination.Coordinates.Latitude,
                    Longitude = destination.Coordinates.Longitude
                }
            };
        }

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }
    }
}