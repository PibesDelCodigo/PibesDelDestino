using Microsoft.AspNetCore.Authorization;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Favorites
{

    // Este servicio se encarga de manejar las operaciones relacionadas con los destinos favoritos de los usuarios.
    [Authorize]
    public class FavoriteAppService : ApplicationService, IFavoriteAppService
    {
        // Inyectamos los repositorios necesarios para acceder a los datos de favoritos y destinos.
        private readonly IRepository<FavoriteDestination, Guid> _repository;
        private readonly IRepository<Destination, Guid> _destinationRepository;

        public FavoriteAppService(
            IRepository<FavoriteDestination, Guid> repository,
            IRepository<Destination, Guid> destinationRepository) 
        {
            _repository = repository;
            _destinationRepository = destinationRepository;
        }

        // Este método se encarga de alternar el estado de favorito de un destino para el usuario actual.
        public async Task<bool> ToggleAsync(CreateFavoriteDto input)
        {
            var existingFavorite = await _repository.FindAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );

            if (existingFavorite != null)
            {
                await _repository.DeleteAsync(existingFavorite);
                return false;
            }
            else
            {
                await _repository.InsertAsync(new FavoriteDestination(
                    GuidGenerator.Create(),
                    CurrentUser.Id.Value,
                    input.DestinationId
                ));
                return true;
            }
        }

        // Este método verifica si un destino específico es favorito para el usuario actual.
        public async Task<bool> IsFavoriteAsync(CreateFavoriteDto input)
        {
            return await _repository.AnyAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );
        }

        public async Task<List<DestinationDto>> GetMyFavoritesAsync()
        {
            // Obtenemos los IQueryable sin ejecutar la consulta aún.
            var favoritesQuery = await _repository.GetQueryableAsync();
            var destinationsQuery = await _destinationRepository.GetQueryableAsync();

            // Construimos una consulta LINQ con JOIN.
            // Esto le dice a SQL Server: "Juntá la tabla de Favoritos con la de Destinos
            // donde coincidan los IDs, filtrá por MI usuario, y dame solo los datos del Destino".
            var query = from fav in favoritesQuery
                        join dest in destinationsQuery on fav.DestinationId equals dest.Id
                        where fav.UserId == CurrentUser.Id
                        select dest;

            // Ejecutamos la consulta.
            // AsyncExecuter se encarga de traducir esto a SQL y traer la lista optimizada.
            var destinations = await AsyncExecuter.ToListAsync(query);
            return ObjectMapper.Map<List<Destination>, List<DestinationDto>>(destinations);
        }
    }
}