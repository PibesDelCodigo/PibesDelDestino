using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Volo.Abp.Users;
using Xunit;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace PibesDelDestino.Ratings
{
    public abstract class RatingAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly IRatingAppService _ratingAppService;
        protected readonly IRepository<Rating, Guid> _ratingRepository;
        protected readonly ICurrentUser _currentUser;

        protected RatingAppService_Tests()
        {
            _ratingAppService = GetRequiredService<IRatingAppService>();
            _ratingRepository = GetRequiredService<IRepository<Rating, Guid>>();
            _currentUser = GetRequiredService<ICurrentUser>();
        }

        // =================================================================================
        // TESTS DE CREACIÓN
        // =================================================================================

        [Fact]
        public async Task CreateOrUpdateAsync_Should_Create_New_If_Not_Exists()
        {
            // Arrange
            var destId = Guid.NewGuid();
            var input = new CreateRatingDto { DestinationId = destId, Score = 5, Comment = "Nuevo" };

            // Act
            var result = await _ratingAppService.CreateAsync(input);

            // Assert
            result.ShouldNotBeNull();
            result.Score.ShouldBe(5);

            var inDb = await _ratingRepository.FindAsync(x => x.DestinationId == destId);
            inDb.ShouldNotBeNull();
            inDb.Score.ShouldBe(5);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_Should_Fail_If_Already_Exists()
        {
            // Arrange
            var destId = Guid.NewGuid();
            var myId = _currentUser.Id.Value;

            // Insertamos una reseña previa directamente para activar el escudo
            await _ratingRepository.InsertAsync(new Rating(Guid.NewGuid(), destId, myId, 1, "Malo"), autoSave: true);

            var input = new CreateRatingDto { DestinationId = destId, Score = 5, Comment = "Intento duplicado" };

            // Act & Assert
            // Verificamos que ahora el método arroja la excepción UserFriendlyException
            var exception = await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(async () =>
            {
                await _ratingAppService.CreateAsync(input);
            });

            exception.Message.ShouldBe("Ya hiciste una reseña para este destino.");
        }

        // =================================================================================
        // TESTS DE ACTUALIZACIÓN (Los tres puntitos)
        // =================================================================================

        [Fact]
        public async Task UpdateAsync_Should_Update_Existing_Rating()
        {
            // Arrange
            var destId = Guid.NewGuid();
            var myId = _currentUser.Id.Value;
            var ratingId = Guid.NewGuid();

            // 1. Insertamos una reseña inicial directamente en la base de datos
            var originalRating = new Rating(ratingId, destId, myId, 2, "Original");
            await _ratingRepository.InsertAsync(originalRating, autoSave: true);

            // 2. Preparamos los datos de edición
            var updateInput = new CreateRatingDto
            {
                DestinationId = destId,
                Score = 5,
                Comment = "Comentario Editado"
            };

            // Act
            var result = await _ratingAppService.UpdateAsync(ratingId, updateInput);

            // Assert
            result.ShouldNotBeNull();
            result.Score.ShouldBe(5);
            result.Comment.ShouldBe("Comentario Editado");

            // Verificamos que en la DB realmente se haya actualizado
            var inDb = await _ratingRepository.GetAsync(ratingId);
            inDb.Score.ShouldBe(5);
            inDb.Comment.ShouldBe("Comentario Editado");
        }

        // =================================================================================
        // TESTS DE CONSULTA
        // =================================================================================

        [Fact]
        public async Task GetMyRatingAsync_Should_Return_Null_If_No_Rating()
        {
            // Act
            var result = await _ratingAppService.GetMyRatingAsync(Guid.NewGuid());

            // Assert
            result.ShouldBeNull();
        }
    }
}