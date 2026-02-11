using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Destinations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using PibesDelDestino.Experiences;
using PibesDelDestino.Notifications;
using Microsoft.AspNetCore.Authorization; // Importante para el Manager

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService :
        CrudAppService<Destination, DestinationDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateDestinationDto>,
        IDestinationAppService
    {
        private readonly ICitySearchService _citySearchService;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;
        private readonly NotificationManager _notificationManager; // Inyectamos el Manager

        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            IRepository<TravelExperience, Guid> experienceRepository,
            NotificationManager notificationManager)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _guidGenerator = guidGenerator;
            _experienceRepository = experienceRepository;
            _notificationManager = notificationManager;
        }

        protected override async Task<List<DestinationDto>> MapToGetListOutputDtosAsync(List<Destination> entities)
        {
            var dtos = await base.MapToGetListOutputDtosAsync(entities);
            var destinationIds = entities.Select(x => x.Id).ToList();

            var query = await _experienceRepository.GetQueryableAsync();
            var allRatings = query
                .Where(x => destinationIds.Contains(x.DestinationId))
                .Select(x => new { x.DestinationId, x.Rating })
                .ToList();

            foreach (var dto in dtos)
            {
                var specificRatings = allRatings
                    .Where(x => x.DestinationId == dto.Id)
                    .Select(x => x.Rating)
                    .ToList();

                if (specificRatings.Any())
                {
                    dto.AverageRating = specificRatings.Average();
                }
                else
                {
                    dto.AverageRating = 0;
                }
            }

            return dtos;
        }

        protected override async Task<DestinationDto> MapToGetOutputDtoAsync(Destination entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);

            var query = await _experienceRepository.GetQueryableAsync();
            var ratings = query.Where(x => x.DestinationId == entity.Id);

            if (await AsyncExecuter.AnyAsync(ratings))
            {
                dto.AverageRating = await AsyncExecuter.AverageAsync(ratings, x => x.Rating);
            }
            else
            {
                dto.AverageRating = 0;
            }

            return dto;
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

            return new DestinationDto
            {
                Id = destination.Id,
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
                },
                AverageRating = 0
            };
        }

        public override async Task<DestinationDto> UpdateAsync(Guid id, CreateUpdateDestinationDto input)
        {
            // 1. Actualizamos el destino
            var updatedDestinationDto = await base.UpdateAsync(id, input);

            // 2. Recuperamos la entidad para pasarla al Manager
            var destinationEntity = await Repository.GetAsync(id);

            // 3. Usamos el Manager para notificar
            await _notificationManager.NotifyDestinationUpdateAsync(
                destinationEntity,
                "información actualizada"
            );

            return updatedDestinationDto;
        }
        [AllowAnonymous]
        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }

        public async Task<List<DestinationDto>> GetTopDestinationsAsync()
        {
            var destinationsQuery = await Repository.GetQueryableAsync();
            var experiencesQuery = await _experienceRepository.GetQueryableAsync();

            var query = from dest in destinationsQuery
                        join exp in experiencesQuery on dest.Id equals exp.DestinationId into ratings
                        where ratings.Any()
                        let avg = ratings.Average(r => r.Rating)
                        orderby avg descending
                        select new
                        {
                            Destination = dest,
                            AverageRating = avg
                        };

            var topList = await AsyncExecuter.ToListAsync(query.Take(10));

            return topList.Select(item => new DestinationDto
            {
                Id = item.Destination.Id,
                Name = item.Destination.Name,
                Country = item.Destination.Country,
                City = item.Destination.City,
                Population = item.Destination.Population,
                Photo = item.Destination.Photo,
                UpdateDate = item.Destination.UpdateDate,
                Coordinates = new CoordinatesDto
                {
                    Latitude = item.Destination.Coordinates.Latitude,
                    Longitude = item.Destination.Coordinates.Longitude
                },
                AverageRating = item.AverageRating
            }).ToList();
        }
    }
}