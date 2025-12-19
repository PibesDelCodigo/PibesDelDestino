using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

// Namespaces de tu proyecto
using PibesDelDestino;
using PibesDelDestino.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using PibesDelDestino.Experiences;
using PibesDelDestino.Application.Contracts.Destinations;

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly IDestinationAppService _destinationService;

        // Definición de Mocks para las 6 dependencias
        private readonly IRepository<Destination, Guid> _destinationRepoMock;
        private readonly ICitySearchService _citySearchServiceMock;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepoMock;
        private readonly IRepository<AppNotification, Guid> _notificationRepoMock;
        private readonly IRepository<TravelExperience, Guid> _experienceRepoMock;
        private readonly IGuidGenerator _guidGenerator;

        public DestinationAppService_Tests()
        {
            // 1. Inicialización de Mocks
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();
            _citySearchServiceMock = Substitute.For<ICitySearchService>();
            _favoriteRepoMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepoMock = Substitute.For<IRepository<AppNotification, Guid>>();
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();

            // Usamos el servicio real de GuidGenerator de ABP
            _guidGenerator = GetRequiredService<IGuidGenerator>();

            // 2. Blindaje de Repositorios
            // Configuramos una lista vacía para evitar errores de 'null' en los cálculos de promedios (LINQ)
            var emptyExperiences = new List<TravelExperience>().AsQueryable();
            _experienceRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyExperiences));

            var emptyDestinations = new List<Destination>().AsQueryable();
            _destinationRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyDestinations));

            // 3. Instancia manual del Proxy (Inyección Manual para evitar error de Autofac)
            _destinationService = new DestinationAppServiceTestProxy(
                _destinationRepoMock,
                _citySearchServiceMock,
                _guidGenerator,
                _favoriteRepoMock,
                _notificationRepoMock,
                _experienceRepoMock,
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Create_Destination_Successfully()
        {
            // ARRANGE
            var input = new CreateUpdateDestinationDto
            {
                Name = "Mendoza",
                Country = "Argentina",
                City = "Mendoza City",
                Population = 120000,
                Photo = "mendoza.jpg",
                UpdateDate = DateTime.Now,
                Coordinates = new CoordinatesDto { Latitude = -32.8895f, Longitude = -68.8458f }
            };

            // ACT
            var result = await _destinationService.CreateAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Name.ShouldBe("Mendoza");
            await _destinationRepoMock.Received(1).InsertAsync(Arg.Any<Destination>());
        }

        [Fact]
        public async Task SearchCitiesAsync_Should_Return_Cities()
        {
            // ARRANGE
            var fakeResult = new CityResultDto
            {
                Cities = new List<CityDto>
                {
                    new CityDto { Name = "London", Country = "UK" }
                }
            };

            _citySearchServiceMock.SearchCitiesAsync(Arg.Any<CityRequestDTO>())
                .Returns(Task.FromResult(fakeResult));

            // ACT
            var input = new CityRequestDTO { PartialName = "Lon" };
            var result = await _destinationService.SearchCitiesAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Cities.Count.ShouldBeGreaterThan(0);
            result.Cities[0].Name.ShouldBe("London");
        }

        [Fact]
        public async Task GetTopDestinationsAsync_Should_Return_List()
        {
            // ACT
            var result = await _destinationService.GetTopDestinationsAsync();

            // ASSERT
            result.ShouldNotBeNull();
            // Retorna lista vacía por el blindaje de arriba, lo cual es un comportamiento correcto en el test
            result.ShouldBeEmpty();
        }
    }

    // Proxy para inyectar manualmente las dependencias y habilitar el ObjectMapper
    public class DestinationAppServiceTestProxy : DestinationAppService
    {
        public DestinationAppServiceTestProxy(
            IRepository<Destination, Guid> repository,
            ICitySearchService citySearchService,
            IGuidGenerator guidGenerator,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IRepository<TravelExperience, Guid> experienceRepository,
            IServiceProvider serviceProvider)
            : base(
                repository,
                citySearchService,
                guidGenerator,
                favoriteRepository,
                notificationRepository,
                experienceRepository)
        {
            // Fundamental para que el CrudAppService pueda usar ObjectMapper, CurrentUser, etc.
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}