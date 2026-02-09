// REQUERIMIENTO 4.4: Consultar promedio de calificaciones.
// Al cargar el detalle del destino, consultamos el repositorio de Experiencias,
// calculamos el promedio de estrellas y lo inyectamos en el DTO.
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
            //REQ 4.4 Promedio de calificaciones
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
        //REQ 2.3 Obtener información detallada de un destino, incluyendo su calificación promedio.
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
        // REQUERIMIENTO 2.3 y 2.5: Guardar Destino y Detalle.
  
        public override async Task<DestinationDto> CreateAsync(CreateUpdateDestinationDto input)
        {//por que sobreescribimos el metodo createAsync? lo hicimos ya que
        // el mapeo manual de la entidad destination tiene un constructor con validaciones
        // entonces si le decimos al automapper va a intentar crear un objeto vacio y 
        // setear propiedades, pero no va a poder por los private set, ademas aca seteamos el VO
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
            //REQ 2.5
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

        //REQ 6.2 Notificar sobre cambios relevantes en destinos seguidos.
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
                    "Actualización de Destino ",
                    $"Hubo cambios recientes en la información de {input.Name}. ¡Revisa los detalles!",
                    "DestinationUpdate"
                ));
            }

            if (notifications.Any())
            { //optimizacion : Insertamos todas las notificaciones de una sola vez en lugar de hacer múltiples llamadas a la base de datos
                await _notificationRepository.InsertManyAsync(notifications);
            }

            return updatedDestination;
        }

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }

        // NUEVO MÉTODO AGREGADO: TOP DESTINOS POPULARES 
        public async Task<List<DestinationDto>> GetTopDestinationsAsync()
        {
            // 1. Obtenemos las consultas base
            var destinationsQuery = await Repository.GetQueryableAsync();
            var experiencesQuery = await _experienceRepository.GetQueryableAsync();

            // 2. LINQ: por cada destino juntame todas las experiencias en una bolsita ratings
            var query = from dest in destinationsQuery
                        join exp in experiencesQuery on dest.Id equals exp.DestinationId into ratings
                        // Solo destinos con al menos 1 voto
                        where ratings.Any() 
                        // variable temporal (avg) para decirle a SQL ordename la lista de mayor a menor
                        let avg = ratings.Average(r => r.Rating)
                        orderby avg descending
                        //Creamos un objeto hibrido anonimo, destino + promedio
                        select new
                        {
                            Destination = dest,
                            AverageRating = avg
                        };

            // 3. Ejecutamos (Top 10), Recien aca va a la base de datos
            var topList = await AsyncExecuter.ToListAsync(query.Take(10));

            // 4. Transforma los objetos hibridos en DestinationDto, para devolverlos
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