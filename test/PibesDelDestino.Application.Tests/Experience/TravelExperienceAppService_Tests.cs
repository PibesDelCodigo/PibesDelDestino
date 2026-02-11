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
using PibesDelDestino.Destinations; // Necesario si el Manager usa DestinationRepo

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

        public TravelExperienceAppService_Tests()
        {
            // 1. Inicialización de los Mocks
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();
            _userRepoMock = Substitute.For<IRepository<IdentityUser, Guid>>();
            _favoriteRepoMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepoMock = Substitute.For<IRepository<AppNotification, Guid>>();
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();

            // 2. CREAMOS EL NOTIFICATION MANAGER
            var notificationManager = new NotificationManager(
                _notificationRepoMock,
                _favoriteRepoMock,
                _destinationRepoMock
            );

            // 3. Instanciamos el Proxy del Servicio con la nueva estructura
            _experienceService = new TravelExperienceAppServiceTestProxy(
                _experienceRepoMock,
                _userRepoMock,
                notificationManager, // <--- Pasamos el Manager
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Create_And_Filter_Experiences()
        {
            // --- ARRANGE ---
            var destinationId = Guid.NewGuid();

            var emptyExperiences = new List<TravelExperience>();
            var emptyFavorites = new List<FavoriteDestination>(); // Lista vacía = nadie recibe noti (está bien para el test)
            var emptyUsers = new List<IdentityUser>();

            // Configuración de GetQueryableAsync
            _experienceRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyExperiences.AsQueryable()));

            // Configuración para el Manager (evitar nulls)
            _favoriteRepoMock.GetListAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>())
                .Returns(Task.FromResult(emptyFavorites));

            // --- ACT ---
            var input = new CreateUpdateTravelExperienceDto
            {
                DestinationId = destinationId,
                Title = "Viaje de prueba",
                Description = "Descripción de la experiencia",
                Rating = 5,
                Date = DateTime.Now
            };

            var result = await _experienceService.CreateAsync(input);

            // --- ASSERT ---
            result.ShouldNotBeNull();
            result.Title.ShouldBe("Viaje de prueba");

            // Verificamos que se llamó al Insert del repositorio principal
            await _experienceRepoMock.Received(1).InsertAsync(Arg.Any<TravelExperience>());

            // Opcional: Verificar que el Manager intentó buscar favoritos (indirectamente prueba que se llamó)
            await _favoriteRepoMock.Received().GetListAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>());
        }
    }

    // Proxy Actualizado
    public class TravelExperienceAppServiceTestProxy : TravelExperienceAppService
    {
        public TravelExperienceAppServiceTestProxy(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            NotificationManager notificationManager, // <--- Recibimos Manager
            IServiceProvider serviceProvider)
            : base(repository, userRepository, notificationManager) // <--- Pasamos Manager al base
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        }
    }
}