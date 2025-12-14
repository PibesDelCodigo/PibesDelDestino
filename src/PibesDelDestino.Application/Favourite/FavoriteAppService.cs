using Microsoft.AspNetCore.Authorization;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations; // <--- Importar esto
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

        public async Task<bool> IsFavoriteAsync(CreateFavoriteDto input)
        {
            return await _repository.AnyAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );
        }

        // --- NUEVO MÉTODO: Traer mis favoritos ---
        public async Task<List<DestinationDto>> GetMyFavoritesAsync()
        {
            // 1. Busco mis IDs de favoritos
            var myFavorites = await _repository.GetListAsync(x => x.UserId == CurrentUser.Id);

            if (!myFavorites.Any())
            {
                return new List<DestinationDto>();
            }

            var destIds = myFavorites.Select(x => x.DestinationId).ToArray();

            // 2. Busco los objetos Destino completos usando esos IDs
            var destinations = await _destinationRepository.GetListAsync(x => destIds.Contains(x.Id));

            // 3. Mapeo a DTO para devolver al frontend
            return ObjectMapper.Map<List<Destination>, List<DestinationDto>>(destinations);
        }
    }
}