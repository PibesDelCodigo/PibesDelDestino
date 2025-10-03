using PibesDelDestino;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace PibesDelDestino.Destinations
{
    public abstract class DestinationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
    {
        private readonly IDestinationAppService _destinationAppService;
        private readonly IRepository<Destination, Guid> _destinationRepository;

        protected DestinationAppService_Tests()
        {
            _destinationAppService = GetRequiredService<IDestinationAppService>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
        }

        [Fact]
        public async Task Should_Create_Destination_Successfully()
        {
            //Arrange
            var input = new CreateUpdateDestinationDto
            {
                Name = "Test Destination",
                Country = "Test Country",
                City = "Test City",
                Population = 100000,
                Photo = "test_photo.jpg",
                UpdateDate = DateTime.UtcNow,
                Coordinates = new CoordinatesDto { Latitude = 40.7128f, Longitude = -74.0060f }
            };

            //Act
            var result = await _destinationAppService.CreateAsync(input);

            //Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Name.ShouldBe(input.Name);
            result.Country.ShouldBe(input.Country);
            result.City.ShouldBe(input.City);
            result.Population.ShouldBe(input.Population);
            result.Photo.ShouldBe(input.Photo);
            result.UpdateDate.ShouldBe(input.UpdateDate);
            result.Coordinates.Latitude.ShouldBe(input.Coordinates.Latitude);
            result.Coordinates.Longitude.ShouldBe(input.Coordinates.Longitude);

            var savedDestination = await _destinationRepository.GetAsync(result.Id);
            savedDestination.ShouldNotBeNull();
            savedDestination.Name.ShouldBe(input.Name);
            savedDestination.Coordinates.Latitude.ShouldBe(input.Coordinates.Latitude);
        }

        [Fact]
        public async Task Should_Not_Allow_Invalid_Values()
        {
            //Act
            var exception = await Assert.ThrowsAsync<AbpValidationException>(async () =>
            {
                await _destinationAppService.CreateAsync(
                    new CreateUpdateDestinationDto
                    {
                        Name = "",
                        Country = "Test Country",
                        City = "Test City",
                        Population = 100000,
                        Photo = "test_photo.jpg",
                        UpdateDate = DateTime.UtcNow,
                        Coordinates = new CoordinatesDto { Latitude = 100.0f, Longitude = -74.0060f }
                    }
                    );
            });

            //Assert
            exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem == "Name"));
            exception.ValidationErrors.ShouldContain(err => err.MemberNames.Contains("Coordinates.Latitude"));
        }

        [Fact]
        public async Task Should_Respond_Expectedly_With_Valid_Input()
        {
            //Arrange
            var input = new CreateUpdateDestinationDto
            {
                Name = "Valid Destination",
                Country = "Valid Country",
                City = "Valid City",
                Population = 200000,
                Photo = "valid_photo.jpg",
                UpdateDate = DateTime.UtcNow,
                Coordinates = new CoordinatesDto { Latitude = 48.8566f, Longitude = 2.3522f }
            };

            //Act
            var result = await _destinationAppService.CreateAsync(input);

            //Assert
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.CreationTime.ShouldNotBe(default);
        }
    }
}