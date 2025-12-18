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
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using PibesDelDestino.Experiences;

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService :
        CrudAppService<Destination, DestinationDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateDestinationDto>,
    IDestinationAppService
    {
        private readonly ICitySearchService _citySearchService;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;

        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IRepository<TravelExperience, Guid> experienceRepository)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _guidGenerator = guidGenerator;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
            _experienceRepository = experienceRepository;
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
            var updatedDestination = await base.UpdateAsync(id, input);

            var followers = await _favoriteRepository.GetListAsync(x => x.DestinationId == id);
            var notifications = new List<AppNotification>();

            foreach (var follow in followers)
            {
                notifications.Add(new AppNotification(
                    _guidGenerator.Create(),
                    follow.UserId,
                    "Actualización de Destino 📢",
                    $"Hubo cambios recientes en la información de {input.Name}. ¡Revisa los detalles!",
                    "DestinationUpdate"
                ));
            }

            if (notifications.Any())
            {
                await _notificationRepository.InsertManyAsync(notifications);
            }

            return updatedDestination;
        }

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }

        // 👇 NUEVO MÉTODO AGREGADO: TOP DESTINOS POPULARES 🏆
        public async Task<List<DestinationDto>> GetTopDestinationsAsync()
        {
            // 1. Obtenemos las consultas base
            var destinationsQuery = await Repository.GetQueryableAsync();
            var experiencesQuery = await _experienceRepository.GetQueryableAsync();

            // 2. LINQ: Unimos, agrupamos, promediamos y ordenamos
            var query = from dest in destinationsQuery
                        join exp in experiencesQuery on dest.Id equals exp.DestinationId into ratings
                        where ratings.Any() // Solo destinos con al menos 1 voto
                        let avg = ratings.Average(r => r.Rating)
                        orderby avg descending
                        select new
                        {
                            Destination = dest,
                            AverageRating = avg
                        };

            // 3. Ejecutamos (Top 10)
            var topList = await AsyncExecuter.ToListAsync(query.Take(10));

            // 4. Mapeamos a DTO para devolver al Frontend
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
                AverageRating = item.AverageRating // ¡El promedio real calculado!
            }).ToList();
        }
    }
}