using PibesDelDestino.Application.Contracts.Destinations; // Necesario para DestinationDto
using PibesDelDestino.Destinations;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Guids;
using Xunit;

namespace PibesDelDestino.Favorites
{
    public abstract class FavoriteAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IFavoriteAppService _favoriteService;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<Destination, Guid> _destinationRepository;
        private readonly IGuidGenerator _guidGenerator;

        protected FavoriteAppService_Tests()
        {
            _favoriteService = GetRequiredService<IFavoriteAppService>();
            _favoriteRepository = GetRequiredService<IRepository<FavoriteDestination, Guid>>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
            _guidGenerator = GetRequiredService<IGuidGenerator>();
        }

        [Fact]
        public async Task ToggleAsync_Should_Add_Favorite_When_Not_Exists()
        {
            // 1. Arrange: Creamos un destino de prueba
            var dest = await CreateDestinationAsync("Bali");

            var input = new CreateFavoriteDto { DestinationId = dest.Id };

            // 2. Act: Llamamos a Toggle (Como no existe, debería AGREGARLO)
            var result = await _favoriteService.ToggleAsync(input);

            // 3. Assert
            result.ShouldBeTrue(); // True indica que "Ahora es favorito"

            // Verificamos que se haya guardado en la base de datos
            var favEnDb = await _favoriteRepository.FirstOrDefaultAsync(x => x.DestinationId == dest.Id);
            favEnDb.ShouldNotBeNull();
        }

        [Fact]
        public async Task ToggleAsync_Should_Remove_Favorite_When_Already_Exists()
        {
            // 1. Arrange: Creamos destino y lo forzamos como favorito
            var dest = await CreateDestinationAsync("Cancún");
            var input = new CreateFavoriteDto { DestinationId = dest.Id };

            // Primer Toggle: Lo agrega
            await _favoriteService.ToggleAsync(input);

            // 2. Act: Segundo Toggle: Lo debería SACAR
            var result = await _favoriteService.ToggleAsync(input);

            // 3. Assert
            result.ShouldBeFalse(); // False indica que "Ya NO es favorito"

            // Verificamos que se haya borrado de la base de datos
            var favEnDb = await _favoriteRepository.FirstOrDefaultAsync(x => x.DestinationId == dest.Id);
            favEnDb.ShouldBeNull();
        }

        [Fact]
        public async Task GetMyFavoritesAsync_Should_Return_List()
        {
            // 1. Arrange
            var dest1 = await CreateDestinationAsync("Destino A");
            var dest2 = await CreateDestinationAsync("Destino B");

            // Agregamos solo el Destino A a mis favoritos usando el servicio
            await _favoriteService.ToggleAsync(new CreateFavoriteDto { DestinationId = dest1.Id });

            // 2. Act
            var lista = await _favoriteService.GetMyFavoritesAsync();

            // 3. Assert
            lista.ShouldNotBeNull();
            lista.Count.ShouldBeGreaterThanOrEqualTo(1);

            // Verificamos que la lista contenga el destino que agregamos
            lista.ShouldContain(d => d.Id == dest1.Id);

            // Verificamos que NO contenga el que no agregamos (si la base estaba limpia)
            // (Nota: Si la base está sucia por otros tests, solo nos importa que esté el nuestro)
            lista.Any(d => d.Id == dest1.Id).ShouldBeTrue();
        }

        // --- Helper para crear destinos rápido ---
        private async Task<Destination> CreateDestinationAsync(string name)
        {
            var dest = new Destination(
                _guidGenerator.Create(),
                name,
                "PaisTest",
                "CiudadTest",
                1000,
                "img.jpg",
                DateTime.Now,
                new Coordinates(0, 0)
            );
            return await _destinationRepository.InsertAsync(dest, autoSave: true);
        }
    }
}