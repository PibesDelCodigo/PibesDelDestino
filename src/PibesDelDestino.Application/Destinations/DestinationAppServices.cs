using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Destinations;
using System;
using System.Collections.Generic; // <--- Necesario para List<>
using System.Linq; // <--- Necesario para .Any()
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
// 1. IMPORTAMOS LOS NUEVOS NAMESPACES
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService :
        CrudAppService<Destination, DestinationDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateDestinationDto>,
    IDestinationAppService
    {
        private readonly ICitySearchService _citySearchService;
        private readonly IGuidGenerator _guidGenerator;

        // 2. DECLARAMOS LOS NUEVOS REPOSITORIOS
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            // 3. INYECTAMOS EN EL CONSTRUCTOR
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository)
            : base(repository)
        {
            _citySearchService = citySearchService;
            _guidGenerator = guidGenerator;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
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
                }
            };
        }

        // -------------------------------------------------------------
        // 4. SOBRESCRIBIMOS EL UPDATE PARA NOTIFICAR (Requisito 6.2) 🔔
        // -------------------------------------------------------------
        public override async Task<DestinationDto> UpdateAsync(Guid id, CreateUpdateDestinationDto input)
        {
            // A. Primero actualizamos el dato en la base de datos (lógica original)
            var updatedDestination = await base.UpdateAsync(id, input);

            // B. Buscamos a los seguidores de este destino (Favoritos)
            var followers = await _favoriteRepository.GetListAsync(x => x.DestinationId == id);

            // C. Preparamos las notificaciones
            var notifications = new List<AppNotification>();

            foreach (var follow in followers)
            {
                // Creamos la alerta para cada usuario
                notifications.Add(new AppNotification(
                    _guidGenerator.Create(),
                    follow.UserId,
                    "Actualización de Destino 📢", // Título
                    $"Hubo cambios recientes en la información de {input.Name}. ¡Revisa los detalles!", // Mensaje
                    "DestinationUpdate" // Tipo (Para filtrar luego en preferencias)
                ));
            }

            // D. Guardamos las notificaciones masivamente
            if (notifications.Any())
            {
                await _notificationRepository.InsertManyAsync(notifications);
            }

            return updatedDestination;
        }
        // -------------------------------------------------------------

        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            return await _citySearchService.SearchCitiesAsync(request);
        }
    }
}