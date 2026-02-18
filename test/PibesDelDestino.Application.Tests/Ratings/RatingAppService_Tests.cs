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

        [Fact]
        public async Task CreateOrUpdateAsync_Should_Create_New_If_Not_Exists()
        {
            // Arrange
            var destId = Guid.NewGuid();
            var input = new CreateRatingDto { DestinationId = destId, Score = 5, Comment = "Nuevo" };

            // Act
            var result = await _ratingAppService.CreateOrUpdateAsync(input);

            // Assert
            result.ShouldNotBeNull();
            result.Score.ShouldBe(5);

            var inDb = await _ratingRepository.FindAsync(x => x.DestinationId == destId);
            inDb.ShouldNotBeNull();
            inDb.Score.ShouldBe(5);
        }

        [Fact]
        public async Task CreateOrUpdateAsync_Should_Update_If_Already_Exists()
        {
            // Arrange
            var destId = Guid.NewGuid();
            var myId = _currentUser.Id.Value;

            // Insertamos uno previo
            await _ratingRepository.InsertAsync(new Rating(Guid.NewGuid(), destId, myId, 1, "Malo"), autoSave: true);

            var input = new CreateRatingDto { DestinationId = destId, Score = 5, Comment = "Mejorado" };

            // Act
            await _ratingAppService.CreateOrUpdateAsync(input);

            // Assert
            var inDb = await _ratingRepository.GetListAsync(x => x.DestinationId == destId && x.UserId == myId);
            inDb.Count.ShouldBe(1); // No debe haber duplicados
            inDb[0].Score.ShouldBe(5);
            inDb[0].Comment.ShouldBe("Mejorado");
        }

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