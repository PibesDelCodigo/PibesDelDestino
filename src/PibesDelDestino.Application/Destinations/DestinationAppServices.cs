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
using Microsoft.AspNetCore.Authorization;

namespace PibesDelDestino.Destinations
{
    //Heredamos de CrudAppService, ya que nos da las operaciones basicas de CRUD
    public class DestinationAppService :
        CrudAppService<Destination, DestinationDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateDestinationDto>,
        IDestinationAppService
    {
        // Inyectamos los servicios necesarios
        private readonly ICitySearchService _citySearchService;
        private readonly IRepository<TravelExperience, Guid> _experienceRepository;
        private readonly INotificationManager _notificationManager;

        // Constructor para inyectar dependencias
        public DestinationAppService(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            IRepository<TravelExperience, Guid> experienceRepository,
            INotificationManager notificationManager)
            : base(repository)
        {
            // Asignamos los servicios inyectados a campos privados para su uso posterior
            _citySearchService = citySearchService;
            _experienceRepository = experienceRepository;
            _notificationManager = notificationManager;
        }

        // Sobrescribimos el método para mapear la lista de entidades a DTOs, incluyendo el cálculo del rating promedio
        //Una vez ya tenemos la lista de destinos, llamamos a este método para mapear cada destino a su DTO correspondiente.
        protected override async Task<List<DestinationDto>> MapToGetListOutputDtosAsync(List<Destination> entities)
        {
            var dtos = await base.MapToGetListOutputDtosAsync(entities);

            if (!entities.Any()) return dtos; // Si no hay destinos, devolvemos la lista vacía de DTOs
           
            var destinationIds = entities.Select(x => x.Id).ToList();
            // Para evitar hacer una consulta por cada destino, obtenemos todas las experiencias relacionadas con los destinos en una sola consulta
            var query = await _experienceRepository.GetQueryableAsync();
            // Obtenemos todas las calificaciones de experiencias para los destinos que estamos mapeando
            var allRatings = query
                .Where(x => destinationIds.Contains(x.DestinationId))
                .Select(x => new { x.DestinationId, x.Rating })
                .ToList();

            // Luego, para cada DTO, calculamos el rating promedio utilizando las calificaciones obtenidas

            foreach (var dto in dtos)
            {
                var specificRatings = allRatings
                    .Where(x => x.DestinationId == dto.Id)
                    .Select(x => x.Rating)
                    .ToList();

                dto.AverageRating = specificRatings.Any() ? specificRatings.Average() : 0;
            }

            return dtos;
        }

        // Sobrescribimos el método para mapear una sola entidad a su DTO, incluyendo el cálculo del rating promedio
        protected override async Task<DestinationDto> MapToGetOutputDtoAsync(Destination entity)
        {
            // Primero, llamamos al método base para obtener el DTO básico
            var dto = await base.MapToGetOutputDtoAsync(entity);
            var query = await _experienceRepository.GetQueryableAsync();
            var ratings = query.Where(x => x.DestinationId == entity.Id);

            //Verificamos si hay calificaciones para este destino y calculamos el promedio
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

        // Sobrescribimos el método de creación para manejar la lógica personalizada al crear un destino
        public override async Task<DestinationDto> CreateAsync(CreateUpdateDestinationDto input)
        {
            var destination = new Destination(
                GuidGenerator.Create(),
                input.Name,
                input.Country,
                input.City,
                input.Population,
                input.Photo,
                input.UpdateDate,
                new Coordinates(input.Coordinates.Latitude, input.Coordinates.Longitude)
            );

            // Insertamos el nuevo destino en el repositorio
            await Repository.InsertAsync(destination);

            var dto = ObjectMapper.Map<Destination, DestinationDto>(destination);
            dto.AverageRating = 0;
            return dto;
        }

        // Sobrescribimos el método de actualización para manejar la lógica personalizada al actualizar un destino

        public override async Task<DestinationDto> UpdateAsync(Guid id, CreateUpdateDestinationDto input)
        {
            //Actualizamos el destino
            var updatedDto = await base.UpdateAsync(id, input);

            //Recuperamos la entidad para pasarla al Manager
            var destinationEntity = await Repository.GetAsync(id);

            //Usamos el Manager para notificar
            await _notificationManager.NotifyDestinationUpdateAsync(
                destinationEntity,
                "información actualizada"
            );

            //Devolvemos el DTO actualizado
            return updatedDto;
        }

        //Agregamos un nuevo método para buscar ciudades utilizando el servicio de búsqueda de ciudades, y lo marcamos con [AllowAnonymous] para permitir el acceso sin autenticación
        [AllowAnonymous]
        public async Task<CityResultDto> SearchCitiesAsync(CityRequestDTO request)
        {
            //Llamamos al servicio de búsqueda de ciudades (El que esta en geodb) para 
            //obtener los resultados basados en el request proporcionado
            return await _citySearchService.SearchCitiesAsync(request);
        }

        //Agregamos un nuevo método para obtener los destinos mejor calificados, ordenados por su rating promedio de mayor a menor, y limitados a los 10 mejores
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

            // Tomamos los 10 destinos mejor calificados y los convertimos a una lista
            var topList = await AsyncExecuter.ToListAsync(query.Take(10));

            // Mapeamos cada destino a su DTO correspondiente, incluyendo el rating promedio calculado
            return topList.Select(item =>
            {
             var dto =  ObjectMapper.Map<Destination, DestinationDto>(item.Destination);
                dto.AverageRating = item.AverageRating;
                return dto;
            }).ToList();
        }
    }
}