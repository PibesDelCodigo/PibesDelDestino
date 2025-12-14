using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Favorites
{
    [Authorize] // Solo usuarios logueados pueden tener favoritos
    public class FavoriteAppService : ApplicationService, IFavoriteAppService
    {
        private readonly IRepository<FavoriteDestination, Guid> _repository;

        public FavoriteAppService(IRepository<FavoriteDestination, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<bool> ToggleAsync(CreateFavoriteDto input)
        {
            // 1. Buscamos si YA existe el like de ESTE usuario a ESTE destino
            var existingFavorite = await _repository.FindAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );

            // 2. LÓGICA DE INTERRUPTOR
            if (existingFavorite != null)
            {
                // YA EXISTÍA -> LO BORRAMOS (Delete)
                await _repository.DeleteAsync(existingFavorite);
                return false; // Devolvemos false (ya no es favorito)
            }
            else
            {
                // NO EXISTÍA -> LO CREAMOS (Create)
                await _repository.InsertAsync(new FavoriteDestination(
                    GuidGenerator.Create(),
                    CurrentUser.Id.Value,
                    input.DestinationId
                ));
                return true; // Devolvemos true (ahora es favorito)
            }
        }

        // Método auxiliar para saber si el corazón debe estar rojo o vacío al cargar la página
        public async Task<bool> IsFavoriteAsync(CreateFavoriteDto input)
        {
            return await _repository.AnyAsync(x =>
                x.UserId == CurrentUser.Id &&
                x.DestinationId == input.DestinationId
            );
        }
    }
}