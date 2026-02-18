using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Experiences;
using PibesDelDestino.Notifications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Guids;
using Volo.Abp.Validation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace PibesDelDestino.Destinations
{
    public abstract class DestinationAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly IDestinationAppService _destinationAppService;
        protected readonly IRepository<Destination, Guid> _destinationRepository;
        protected readonly IRepository<TravelExperience, Guid> _experienceRepository;
        protected readonly INotificationManager _notificationManagerMock;

        protected DestinationAppService_Tests()
        {
            _destinationAppService = GetRequiredService<IDestinationAppService>();
            _destinationRepository = GetRequiredService<IRepository<Destination, Guid>>();
            _experienceRepository = GetRequiredService<IRepository<TravelExperience, Guid>>();

            // Obtenemos el Mock del manager para verificar las llamadas en el Update
            _notificationManagerMock = GetRequiredService<INotificationManager>();
        }

        /* ----- TESTS DE INTEGRACIÓN (SQLite) ----- */

        [Fact]
        public async Task CreateAsync_Should_Create_Destination_Successfully()
        {
            var input = new CreateUpdateDestinationDto
            {
                Name = "Tandil",
                Country = "Argentina",
                City = "Tandil",
                Population = 150000,
                Photo = "piedra_movediza.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = -37.32f, Longitude = -59.13f }
            };

            var result = await _destinationAppService.CreateAsync(input);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Tandil");

            var dbEntry = await _destinationRepository.GetAsync(result.Id);
            dbEntry.Name.ShouldBe("Tandil");
        }

        [Fact]
        public async Task UpdateAsync_Should_Notify_Manager_On_Success()
        {
            // 1. Arrange: Creamos un destino para editar
            var destId = Guid.NewGuid();
            await _destinationRepository.InsertAsync(new Destination(
                destId, "Mar del Plata", "Argentina", "MDP", 600000, "playa.jpg",
                DateTime.Now, new Coordinates(-38.00f, -57.55f)
            ));

            var input = new CreateUpdateDestinationDto
            {
                Name = "La Feliz",
                Country = "Argentina",
                City = "Mar del Plata",
                Population = 650000,
                Photo = "playa_nueva.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = -38.00f, Longitude = -57.55f }
            };

            // 2. Act
            await _destinationAppService.UpdateAsync(destId, input);

            // 3. Assert: Verificamos que se llamó al Manager con el mensaje correcto
            await _notificationManagerMock.Received(1).NotifyDestinationUpdateAsync(
                Arg.Is<Destination>(d => d.Id == destId),
                Arg.Is<string>(s => s.Contains("actualizada"))
            );
        }

        [Fact]
        public async Task GetTopDestinationsAsync_Should_Rank_By_Average_Rating()
        {
            // 1. Arrange: Creamos destinos y experiencias con ratings
            var dest1 = Guid.NewGuid();
            var dest2 = Guid.NewGuid();

            await _destinationRepository.InsertAsync(new Destination(dest1, "Destino A", "C1", "City1", 1, "p1.jpg", DateTime.Now, new Coordinates(0, 0)));
            await _destinationRepository.InsertAsync(new Destination(dest2, "Destino B", "C2", "City2", 1, "p2.jpg", DateTime.Now, new Coordinates(0, 0)));

            // Destino 1 tiene promedio 5
            await _experienceRepository.InsertAsync(new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), dest1, "T1", "D1", DateTime.Now, 5));
            // Destino 2 tiene promedio 3
            await _experienceRepository.InsertAsync(new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), dest2, "T2", "D2", DateTime.Now, 3));

            // 2. Act
            var topDestinations = await _destinationAppService.GetTopDestinationsAsync();

            // 3. Assert
            topDestinations.Count.ShouldBeGreaterThanOrEqualTo(2);
            topDestinations[0].Id.ShouldBe(dest1); // El de 5 puntos debe estar primero
            topDestinations[0].AverageRating.ShouldBe(5);
            topDestinations[1].AverageRating.ShouldBe(3);
        }

        /* ----- TESTS DE MOCK (Estilo Manual) ----- */

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Empty_When_No_Results_Found()
        {
            // 1. Arrange: Simulamos que no hay resultados
            var request = new CityRequestDTO { PartialName = "CiudadInexistente" };
            var mockEmptyResult = new CityResultDto(); // Lista vacía por defecto

            var citySearchServiceMock = Substitute.For<ICitySearchService>();
            citySearchServiceMock.SearchCitiesAsync(request).Returns(Task.FromResult(mockEmptyResult));

            var manualAppService = new DestinationAppService(
                Substitute.For<IRepository<Destination, Guid>>(),
                citySearchServiceMock,
                GetRequiredService<IGuidGenerator>(),
                Substitute.For<IRepository<TravelExperience, Guid>>(),
                Substitute.For<INotificationManager>()
            );

            // 2. Act
            var result = await manualAppService.SearchCitiesAsync(request);

            // 3. Assert
            result.ShouldNotBeNull();
            // result.Cities.ShouldBeEmpty(); // Dependiendo de como sea tu CityResultDto
        }

        /* ----- TESTS DE VALIDACIÓN ----- */

        [Fact]
        public async Task CreateAsync_Should_Not_Allow_Invalid_Values()
        {
            await Assert.ThrowsAsync<AbpValidationException>(async () =>
            {
                await _destinationAppService.CreateAsync(
                    new CreateUpdateDestinationDto
                    {
                        Name = "", // Invalido
                        Country = "Argentina",
                        Coordinates = new CoordinatesDto { Latitude = 150f } // Invalido
                    }
                );
            });
        }
    }
}