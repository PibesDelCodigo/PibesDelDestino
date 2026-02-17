using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
// IMPORTANTE: Estos son los namespaces que suelen faltar
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Emailing;
using Microsoft.Extensions.Logging;

using PibesDelDestino.Experiences;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using PibesDelDestino.Destinations;

namespace PibesDelDestino.Experiences
{
    public class TravelExperienceAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly ITravelExperienceAppService _experienceService;

        // Mocks
        private readonly IRepository<TravelExperience, Guid> _experienceRepoMock;
        private readonly IRepository<IdentityUser, Guid> _userRepoMock;
        private readonly IRepository<Destination, Guid> _destinationRepoMock;

        // Mocks Manager
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepoMock;
        private readonly IRepository<AppNotification, Guid> _notificationRepoMock;
        private readonly IIdentityUserRepository _identityUserRepoMock;
        private readonly IEmailSender _emailSenderMock;
        private readonly ILogger<NotificationManager> _loggerMock;

        public TravelExperienceAppService_Tests()
        {
            // 1. Inicializar Mocks
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();
            _userRepoMock = Substitute.For<IRepository<IdentityUser, Guid>>();
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();

            _favoriteRepoMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepoMock = Substitute.For<IRepository<AppNotification, Guid>>();
            _identityUserRepoMock = Substitute.For<IIdentityUserRepository>();
            _emailSenderMock = Substitute.For<IEmailSender>();
            _loggerMock = Substitute.For<ILogger<NotificationManager>>();

            // 2. Mock del Usuario (CurrentUser)
            var currentUserMock = Substitute.For<ICurrentUser>();
            currentUserMock.Id.Returns(Guid.NewGuid());
            currentUserMock.IsAuthenticated.Returns(true);

            // 3. Mock del LazyServiceProvider (El truco para inyectar propiedades base)
            var lazyProviderMock = Substitute.For<IAbpLazyServiceProvider>();

            // Cuando el servicio pida "ICurrentUser", le damos nuestro mock
            lazyProviderMock.LazyGetRequiredService<ICurrentUser>().Returns(currentUserMock);

            // 4. Crear Manager
            var notificationManager = new NotificationManager(
                _notificationRepoMock,
                _favoriteRepoMock,
                _destinationRepoMock,
                _identityUserRepoMock,
                _emailSenderMock,
                _loggerMock
            );

            // 5. Crear el Servicio (Usando el Proxy)
            // Pasamos 'lazyProviderMock' en lugar del ServiceProvider real
            _experienceService = new TravelExperienceAppServiceTestProxy(
                _experienceRepoMock,
                _userRepoMock,
                _destinationRepoMock,
                notificationManager,
                lazyProviderMock
            );
        }

        [Fact]
        public async Task Should_Create_Experience_Successfully()
        {
            // ARRANGE
            var destId = Guid.NewGuid();
            var fakeDest = new Destination(destId, "Paris", "France", "Paris", 100, "img", DateTime.Now, new Coordinates(0, 0));

            // Configuramos el mock para que devuelva el destino cuando lo busquen
            _destinationRepoMock.GetAsync(destId).Returns(Task.FromResult(fakeDest));

            var input = new CreateUpdateTravelExperienceDto
            {
                DestinationId = destId,
                Title = "Test Trip",
                Description = "Amazing",
                Rating = 5,
                Date = DateTime.Now
            };

            // ACT
            var result = await _experienceService.CreateAsync(input);

            // ASSERT
            result.ShouldNotBeNull();
            result.Title.ShouldBe("Test Trip");
            await _experienceRepoMock.Received(1).InsertAsync(Arg.Any<TravelExperience>());
        }
    }

    // CLASE PROXY CORREGIDA
    public class TravelExperienceAppServiceTestProxy : TravelExperienceAppService
    {
        public TravelExperienceAppServiceTestProxy(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<Destination, Guid> destinationRepository,
            NotificationManager notificationManager,
            IAbpLazyServiceProvider lazyServiceProvider) // Recibimos el mock aquí
            : base(repository, userRepository, destinationRepository, notificationManager)
        {
            // Asignamos el mock a la propiedad base
            LazyServiceProvider = lazyServiceProvider;
        }
    }
}