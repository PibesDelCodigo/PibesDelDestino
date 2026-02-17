using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using PibesDelDestino.Experiences;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using PibesDelDestino.Destinations;
using Volo.Abp.Emailing;
using Microsoft.Extensions.Logging;
using Volo.Abp.Logging;

namespace PibesDelDestino.Experiences
{
    public class TravelExperienceAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly ITravelExperienceAppService _experienceService;

        // Mocks de los repositorios
        private readonly IRepository<TravelExperience, Guid> _experienceRepoMock;
        private readonly IRepository<IdentityUser, Guid> _userRepoMock;

        // Mocks para el Manager
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepoMock;
        private readonly IRepository<AppNotification, Guid> _notificationRepoMock;
        private readonly IRepository<Destination, Guid> _destinationRepoMock; // El Manager lo necesita
        private readonly IIdentityUserRepository _identityUserRepoMock;
        private readonly IEmailSender _emailSenderMock;
        private readonly ILogger<NotificationManager> _loggerMock;

        public TravelExperienceAppService_Tests()
        {
            //Inicialización de los Mocks
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();
            _userRepoMock = Substitute.For<IRepository<IdentityUser, Guid>>();
            _favoriteRepoMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepoMock = Substitute.For<IRepository<AppNotification, Guid>>();
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();
            _identityUserRepoMock = Substitute.For<IIdentityUserRepository>();
            _emailSenderMock = Substitute.For<IEmailSender>();
            _loggerMock = Substitute.For<ILogger<NotificationManager>>();

            // Mocks adicionales para el constructor de NotificationManager
            var _emailSenderMock2 = Substitute.For<IEmailSender>();
            var _loggerMock2 = Substitute.For<ILogger<NotificationManager>>();

            //CREAMOS EL NOTIFICATION MANAGER
            var notificationManager = new NotificationManager(
                _notificationRepoMock,
                _favoriteRepoMock,
                _destinationRepoMock,
                _identityUserRepoMock,
                _emailSenderMock,
                _loggerMock
            );

            //Instanciamos el Proxy del Servicio con la nueva estructura
            _experienceService = new TravelExperienceAppServiceTestProxy(
                _experienceRepoMock,
                _userRepoMock,
                notificationManager,
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Create_And_Filter_Experiences()
        {
            var destinationId = Guid.NewGuid();
            var emptyExperiences = new List<TravelExperience>();
            var emptyFavorites = new List<FavoriteDestination>(); 
            var emptyUsers = new List<IdentityUser>();
            _experienceRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyExperiences.AsQueryable()));

            // Configuración para el Manager (evitar nulls)
            _favoriteRepoMock.GetListAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>())
                .Returns(Task.FromResult(emptyFavorites));

            var input = new CreateUpdateTravelExperienceDto
            {
                DestinationId = destinationId,
                Title = "Viaje de prueba",
                Description = "Descripción de la experiencia",
                Rating = 5,
                Date = DateTime.Now
            };

            var result = await _experienceService.CreateAsync(input);

            result.ShouldNotBeNull();
            result.Title.ShouldBe("Viaje de prueba");

            // Verificamos que se llamó al Insert del repositorio principal
            await _experienceRepoMock.Received(1).InsertAsync(Arg.Any<TravelExperience>());
            await _favoriteRepoMock.Received().GetListAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>());
        }
    }
    public class TravelExperienceAppServiceTestProxy : TravelExperienceAppService
    {
        public TravelExperienceAppServiceTestProxy(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            NotificationManager notificationManager,
            IServiceProvider serviceProvider)
            : base(repository, userRepository, notificationManager)
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        }
    }
}