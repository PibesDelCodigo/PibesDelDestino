using NSubstitute;
using PibesDelDestino.Ratings;
using Shouldly;
using System;
using System.Collections.Generic; // Added for List<RatingDto> if needed later
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping; // Needed for IObjectMapper
using Volo.Abp.Users; // Needed for ICurrentUser
using Volo.Abp.Validation; // Needed if testing validation exceptions later
using Xunit;

namespace PibesDelDestino.Ratings
{
    public abstract class RatingAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentUser _currentUser;

        protected RatingAppService_Tests()
        {
            // Get required services from the test DI container
            _guidGenerator = GetRequiredService<IGuidGenerator>();
            _currentUser = GetRequiredService<ICurrentUser>();
        }

        [Fact]
        public async Task CreateAsync_Should_Create_Rating_Correctly()
        {
            // ARRANGE
            var repositoryMock = Substitute.For<IRepository<Rating, Guid>>();
            var objectMapperMock = Substitute.For<IObjectMapper>();

            // Create the service instance, passing all required dependencies
            var ratingAppService = new RatingAppService(
                repositoryMock,
                _guidGenerator, // Pass the real GuidGenerator from the test base
                objectMapperMock, // Pass the mock ObjectMapper
                _currentUser // Pass the real CurrentUser from the test base
            );

            var input = new CreateRatingDto
            {
                DestinationId = Guid.NewGuid(),
                Score = 5,
                Comment = "¡Excelente lugar!"
            };

            // Configure the mock ObjectMapper to return a simulated DTO
            var simulatedDto = new RatingDto
            {
                Id = Guid.NewGuid(), // Simulate that a new ID was generated
                DestinationId = input.DestinationId,
                UserId = _currentUser.Id.Value, // Use the simulated user ID
                Score = input.Score,
                Comment = input.Comment
            };
            objectMapperMock.Map<Rating, RatingDto>(Arg.Any<Rating>()).Returns(simulatedDto);

            // ACT
            var result = await ratingAppService.CreateAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.DestinationId.ShouldBe(input.DestinationId);
            result.Score.ShouldBe(input.Score);
            result.Comment.ShouldBe(input.Comment);
            result.UserId.ShouldBe(_currentUser.Id.Value); // Verify correct UserId assignment

            // Verify that the repository's InsertAsync method was called exactly once
            await repositoryMock.Received(1).InsertAsync(Arg.Is<Rating>(r =>
                r.DestinationId == input.DestinationId &&
                r.Score == input.Score &&
                r.Comment == input.Comment &&
                r.UserId == _currentUser.Id.Value
            ));
        }

        // --- ADD OTHER UNIT TESTS HERE ---
        // Example: Test for score validation
        [Fact]
        public async Task CreateAsync_Should_Throw_Exception_For_Invalid_Score()
        {
            // ARRANGE
            var repositoryMock = Substitute.For<IRepository<Rating, Guid>>();
            var objectMapperMock = Substitute.For<IObjectMapper>();

            var ratingAppService = new RatingAppService(
                repositoryMock,
                _guidGenerator,
                objectMapperMock,
                _currentUser
            );

            var input = new CreateRatingDto
            {
                DestinationId = Guid.NewGuid(),
                Score = 0, // Invalid score
                Comment = "Test"
            };

            // ACT & ASSERT
            // We expect an ArgumentException because the validation is in the Rating entity constructor
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await ratingAppService.CreateAsync(input);
            });
        }
    }
 }