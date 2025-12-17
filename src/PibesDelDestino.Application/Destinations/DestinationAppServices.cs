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
// 👇 1. IMPORTAMOS EXPERIENCIAS PARA LEER LAS CALIFICACIONES
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

        // 👇 2. NUEVO REPOSITORIO DE EXPERIENCIAS
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;

        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            // 👇 3. INYECTAMOS EN EL CONSTRUCTOR
            IRepository<TravelExperience, Guid> experienceRepository)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _guidGenerator = guidGenerator;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
            _experienceRepository = experienceRepository;
        }

        // -----------------------------------------------------------------------
        // ⭐ MAGIA PARA LAS ESTRELLAS: CÁLCULO DE PROMEDIO AL LISTAR ⭐
        // -----------------------------------------------------------------------
        protected override async Task<List<DestinationDto>> MapToGetListOutputDtosAsync(List<Destination> entities)
        {
            // 1. Obtenemos los DTOs básicos (Nombre, Ciudad, etc.)
            var dtos = await base.MapToGetListOutputDtosAsync(entities);

            // 2. Sacamos los IDs de los destinos que estamos listando
            var destinationIds = entities.Select(x => x.Id).ToList();

            // 3. Traemos TODAS las calificaciones de estos destinos de una sola vez (Optimización)
            var query = await _experienceRepository.GetQueryableAsync();
            var allRatings = query
                .Where(x => destinationIds.Contains(x.DestinationId))
                .Select(x => new { x.DestinationId, x.Rating }) // Solo traemos lo necesario
                .ToList();

            // 4. Asignamos el promedio a cada DTO
            foreach (var dto in dtos)
            {
                // Filtramos las notas de este destino específico
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
                    dto.AverageRating = 0; // Si no tiene reseñas, 0 estrellas
                }
            }

            return dtos;
        }

        // ⭐ MAGIA PARA LAS ESTRELLAS: CÁLCULO AL VER DETALLE INDIVIDUAL ⭐
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
        // -----------------------------------------------------------------------

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
                AverageRating = 0 // Al crear es nuevo, tiene 0 estrellas
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
    }
}