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
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Microsoft.Extensions.Logging;
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
        private readonly DestinationAppService _destinationService;

        // Mocks
        private readonly IRepository<Destination, Guid> _destinationRepoMock;
        private readonly ICitySearchService _citySearchServiceMock;
        private readonly IGuidGenerator _guidGeneratorMock;
        private readonly IRepository<TravelExperience, Guid> _experienceRepositoryMock;

        // Estos mocks los necesitamos PARA el Manager
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepositoryMock;
        private readonly IRepository<AppNotification, Guid> _notificationRepositoryMock;
        private readonly IIdentityUserRepository _identityUserRepoMock;
        private readonly IEmailSender _emailSenderMock;
        private readonly ILogger<NotificationManager> _loggerMock;


        public DestinationAppService_Tests()
        {
            // 1. Inicializar Mocks
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();
            _citySearchServiceMock = Substitute.For<ICitySearchService>();
            _guidGeneratorMock = Substitute.For<IGuidGenerator>();
            _experienceRepositoryMock = Substitute.For<IRepository<TravelExperience, Guid>>();

            // Inicializamos estos aunque el AppService no los use directo, el Manager sí
            _favoriteRepositoryMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepositoryMock = Substitute.For<IRepository<AppNotification, Guid>>();

            // 2. Configurar GuidGenerator
            _guidGeneratorMock.Create().Returns(Guid.NewGuid());
            _identityUserRepoMock = Substitute.For<IIdentityUserRepository>();
            _emailSenderMock = Substitute.For<IEmailSender>();
            _loggerMock = Substitute.For<ILogger<NotificationManager>>();

            // 3. Crear el ServiceProvider Mock (para el Lazy)
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IAbpLazyServiceProvider))
                           .Returns(Substitute.For<IAbpLazyServiceProvider>());

            // 4. CREAR EL NOTIFICATION MANAGER (Con los mocks inyectados)
            var notificationManager = new NotificationManager(
                _notificationRepositoryMock,
                _favoriteRepositoryMock,
                _destinationRepoMock,
                _identityUserRepoMock,
                _emailSenderMock,
                _loggerMock,
                _emailSenderMock, // <-- Segundo IEmailSender requerido por el constructor
                _loggerMock       // <-- Segundo ILogger<NotificationManager> requerido por el constructor
            );

            // 5. Instanciar el Servicio (Usando el nuevo constructor)
            _destinationService = new DestinationAppServiceTestProxy(
                _destinationRepoMock,
                _citySearchServiceMock,
                _guidGeneratorMock,
                _experienceRepositoryMock,
                notificationManager, // <--- Pasamos el Manager real (con mocks adentro)
                serviceProvider
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
            IRepository<TravelExperience, Guid> experienceRepository,
            NotificationManager notificationManager, // <--- AGREGADO
            IServiceProvider serviceProvider)
            : base(
                repository,
                citySearchService,
                guidGenerator,
                experienceRepository,
                notificationManager) // <--- PASAMOS EL MANAGER
        {
            // Fundamental para que el CrudAppService pueda usar ObjectMapper, CurrentUser, etc.
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
        }
    }
}