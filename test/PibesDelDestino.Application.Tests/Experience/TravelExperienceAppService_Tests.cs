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

namespace PibesDelDestino.Experiences
{
    public class TravelExperienceAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly ITravelExperienceAppService _experienceService;

        // Mocks de los 4 repositorios requeridos por el constructor
        private readonly IRepository<TravelExperience, Guid> _experienceRepoMock;
        private readonly IRepository<IdentityUser, Guid> _userRepoMock;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepoMock;
        private readonly IRepository<AppNotification, Guid> _notificationRepoMock;

        public TravelExperienceAppService_Tests()
        {
            // 1. Inicialización de los Mocks con NSubstitute
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();
            _userRepoMock = Substitute.For<IRepository<IdentityUser, Guid>>();
            _favoriteRepoMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _notificationRepoMock = Substitute.For<IRepository<AppNotification, Guid>>();

            // 2. Uso del Proxy para inyectar los Mocks y resolver dependencias de la clase base
            _experienceService = new TravelExperienceAppServiceTestProxy(
                _experienceRepoMock,
                _userRepoMock,
                _favoriteRepoMock,
                _notificationRepoMock,
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Create_And_Filter_Experiences()
        {
            // --- ARRANGE ---
            var destinationId = Guid.NewGuid();

            // Listas vacías para evitar errores de referencia nula (ArgumentNullException en .Select)
            var emptyExperiences = new List<TravelExperience>();
            var emptyFavorites = new List<FavoriteDestination>();
            var emptyNotifications = new List<AppNotification>();
            var emptyUsers = new List<IdentityUser>();

            // Configuración de GetQueryableAsync para todos los repositorios
            _experienceRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyExperiences.AsQueryable()));
            _favoriteRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyFavorites.AsQueryable()));
            _notificationRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyNotifications.AsQueryable()));
            _userRepoMock.GetQueryableAsync().Returns(Task.FromResult(emptyUsers.AsQueryable()));

            // Configuración de GetListAsync con predicado (Vital para evitar el error en la línea 118)
            _favoriteRepoMock.GetListAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(emptyFavorites));

            _userRepoMock.GetListAsync(Arg.Any<Expression<Func<IdentityUser, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(emptyUsers));

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
        }
    }

    // Proxy para exponer el LazyServiceProvider de la clase base ApplicationService
    public class TravelExperienceAppServiceTestProxy : TravelExperienceAppService
    {
        public TravelExperienceAppServiceTestProxy(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository,
            IServiceProvider serviceProvider)
            : base(repository, userRepository, favoriteRepository, notificationRepository)
        {
            // Inyectamos el LazyServiceProvider para que ObjectMapper y CurrentUser funcionen correctamente
            LazyServiceProvider = serviceProvider.GetRequiredService<Volo.Abp.DependencyInjection.IAbpLazyServiceProvider>();
        }
    }
}