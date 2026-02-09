// REQUERIMIENTOS 5.1 y 5.2: Gestión de Favoritos.
// Permite al usuario autenticado:
// - 5.1: Agregar un destino a su lista personal de favoritos.
// - 5.2: Eliminar un destino de la lista si ya no le interesa.
//REQUERIMIENTO 5.3: Consultar lista personal.
// Recupera todos los destinos marcados como favoritos por el usuario actual.
// Se utiliza para renderizar la pantalla "Mis Viajes".
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
    [Authorize]
    public class FavoriteAppService : ApplicationService, IFavoriteAppService
    {
        private readonly IRepository<FavoriteDestination, Guid> _repository;
        private readonly IRepository<Destination, Guid> _destinationRepository; // <--- Nuevo Repositorio

        public FavoriteAppService(
            IRepository<FavoriteDestination, Guid> repository,
            IRepository<Destination, Guid> destinationRepository) // <--- Inyección
        {
            _repository = repository;
            _destinationRepository = destinationRepository;
        }
        //REQ 5.1 y 5.2: Agregar y eliminar favoritos (Toggle)
        //implementamos toggle para simplificar la interaccion desde el front
        //es lo que hace si esta el corazon se desmarca y sino se marca.
        public async Task<bool> ToggleAsync(CreateFavoriteDto input)
        {//Aseguramos una relacion unica entre usuario y destino favorito,
         //evitando duplicados
            var existingFavorite = await _repository.FindAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );
            //5.2: Si ya existe, lo elimino
            if (existingFavorite != null)
            {
                await _repository.DeleteAsync(existingFavorite);
                return false;
            }
            else
            { //REQ 5.1: Si no existe, lo creo, Basicamente si existe la
              //destruimos y si no existe la instanciamos 
                await _repository.InsertAsync(new FavoriteDestination(
                    GuidGenerator.Create(),
                    CurrentUser.Id.Value,
                    input.DestinationId
                ));
                return true;
                //los retornos bool son para que el front sepa como pintar
                //true=rojo, false=gris.
            }
        }
        
        public async Task<bool> IsFavoriteAsync(CreateFavoriteDto input)
        {//Utilizamos AnyAsync porque no necesitamos recuperar los datos de la
         //relación ni saber cuántas veces lo marcó. Solo necesitamos un booleano
         //de existencia (Sí/No). Esto genera la consulta SQL más eficiente
         //posible (EXISTS), minimizando el tráfico de red y la carga del
         //servidor de base de datos
            return await _repository.AnyAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );
        }

        //  REQ 5.3 Traer mis favoritos 
        public async Task<List<DestinationDto>> GetMyFavoritesAsync()
        {
            // 1. mis IDs de favoritos
            var myFavorites = await _repository.GetListAsync(x => x.UserId == CurrentUser.Id);

            if (!myFavorites.Any())
            {
                return new List<DestinationDto>();
            }

            var destIds = myFavorites.Select(x => x.DestinationId).ToArray();

            // 2. objetos Destino completos usando esos IDs
            var destinations = await _destinationRepository.GetListAsync(x => destIds.Contains(x.Id));

            // 3. Mapeo a DTO para devolver al frontend
            return ObjectMapper.Map<List<Destination>, List<DestinationDto>>(destinations);
        }
    }
}