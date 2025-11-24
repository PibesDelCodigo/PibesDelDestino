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
                UpdateDate = DateTime.UtcNow,
                Coordinates = new CoordinatesDto { Latitude = 40.7128f, Longitude = -74.0060f }
            };

            // ACT
            var result = await appService.CreateAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Id.ShouldNotBe(Guid.Empty);
            result.Name.ShouldBe("Test Destination");
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
            // Capturamos la excepción 'ArgumentException' que lanza tu Entidad
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var invalidInput = new CreateUpdateDestinationDto
                {
                    Name = "", // Valor inválido (vacío)
                    Country = "Test Country",
                    City = "Test City",
                    Population = 100000,
                    UpdateDate = DateTime.UtcNow,
                    Coordinates = new CoordinatesDto { Latitude = 40.7128f, Longitude = -74.0060f }
                };
                await appService.CreateAsync(invalidInput);
            });

            // Verificamos que el mensaje de error mencione el campo "name"
            // (ABP Check.NotNullOrWhiteSpace suele poner el nombre del parámetro en el mensaje)
            exception.Message.ShouldContain("name", Case.Insensitive);
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Cities_From_Search_Service()
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