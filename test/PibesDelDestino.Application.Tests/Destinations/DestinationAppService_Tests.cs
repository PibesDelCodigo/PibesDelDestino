using NSubstitute;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace PibesDelDestino.Destinations
{
    public abstract class DestinationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
    {
        public DestinationAppService_Tests()
        {
            // Constructor vacío.
        }

        [Fact]
        public async Task Should_Create_Destination_Successfully()
        {
            // ARRANGE
            var citySearchServiceMock = Substitute.For<ICitySearchService>();
            var repositoryMock = Substitute.For<IRepository<Destination, Guid>>();
            var guidGeneratorMock = Substitute.For<IGuidGenerator>();

            // Le enseñamos al mock qué Guid debe devolver
            guidGeneratorMock.Create().Returns(Guid.NewGuid());

            var appService = new DestinationAppService(
                repositoryMock,
                citySearchServiceMock,
                guidGeneratorMock
            );

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

            // ACT
            var result = await appService.CreateAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty); // Esta verificación ahora pasará
        }

        [Fact]
        public async Task Should_Not_Allow_Invalid_Values()
        {
            // ARRANGE
            var citySearchServiceMock = Substitute.For<ICitySearchService>();
            var repositoryMock = Substitute.For<IRepository<Destination, Guid>>();
            var guidGeneratorMock = Substitute.For<IGuidGenerator>();
            var appService = new DestinationAppService(
                repositoryMock,
                citySearchServiceMock,
                guidGeneratorMock
            );

            // ACT & ASSERT
            // --- CAMBIA EL TIPO DE EXCEPCIÓN AQUÍ ---
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var invalidInput = new CreateUpdateDestinationDto
                    {
                    Name = "", // Valor inválido
                        Country = "Test Country",
                        City = "Test City",
                        Population = 100000,
                        Photo = "test_photo.jpg",
                        UpdateDate = DateTime.UtcNow,
                    Coordinates = new CoordinatesDto { Latitude = 40.7128f, Longitude = -74.0060f }
                };
                await appService.CreateAsync(invalidInput);
            });

            //Assert
            exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem == "Name"));
            exception.ValidationErrors.ShouldContain(err => err.MemberNames.Contains("Coordinates.Latitude"));
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Cities_From_Search_Service()
        {
            // ARRANGE
            var citySearchServiceMock = Substitute.For<ICitySearchService>();
            var repositoryMock = Substitute.For<IRepository<Destination, Guid>>();
            var guidGeneratorMock = Substitute.For<IGuidGenerator>(); // Necesario para el constructor

            var appService = new DestinationAppService(
                repositoryMock,
                citySearchServiceMock,
                guidGeneratorMock
            );

            var fakeCities = new CityResultDto
            {
                Cities = new List<CityDto> { new CityDto { Name = "Buenos Aires" } }
            };
            citySearchServiceMock.SearchCitiesAsync(Arg.Any<CityRequestDTO>())
                                 .Returns(Task.FromResult(fakeCities));

            var input = new CityRequestDTO { PartialName = "bue" };

            // ACT
            var result = await appService.SearchCitiesAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Cities.Count.ShouldBe(1);
        }
    }
}