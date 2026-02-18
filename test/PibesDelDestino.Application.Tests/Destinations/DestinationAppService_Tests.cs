using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Volo.Abp.Emailing;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Cities;
using PibesDelDestino.Destinations;
using PibesDelDestino.Experiences;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Xunit;

namespace PibesDelDestino.Destinations
{
    public class DestinationAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly DestinationAppService _destinationService;
        private IRepository<Destination, Guid> _destinationRepoMock;
        private IRepository<TravelExperience, Guid> _experienceRepoMock;
        private ICitySearchService _citySearchServiceMock;
        private NotificationManager _notificationManagerMock;


        public DestinationAppService_Tests()
        {
            _destinationService = GetRequiredService<DestinationAppService>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            // Crear mocks
            _destinationRepoMock = Substitute.For<IRepository<Destination, Guid>>();
            _experienceRepoMock = Substitute.For<IRepository<TravelExperience, Guid>>();
            _citySearchServiceMock = Substitute.For<ICitySearchService>();

            // Reemplazar registros en el contenedor
            services.Replace(ServiceDescriptor.Singleton(typeof(IRepository<Destination, Guid>), _destinationRepoMock));
            services.Replace(ServiceDescriptor.Singleton(typeof(IRepository<TravelExperience, Guid>), _experienceRepoMock));
            services.Replace(ServiceDescriptor.Transient(_ => _citySearchServiceMock));

            // Si el NotificationManager necesita otros mocks, puedes agregarlos aquí
            _notificationManagerMock = Substitute.For<NotificationManager>(
                Substitute.For<IRepository<AppNotification, Guid>>(),
                Substitute.For<IRepository<FavoriteDestination, Guid>>(),
                Substitute.For<IRepository<Destination, Guid>>(),
                Substitute.For<IIdentityUserRepository>(),
                Substitute.For<IEmailSender>(),
                Substitute.For<ILogger<NotificationManager>>()
            );
            services.Replace(ServiceDescriptor.Singleton(_notificationManagerMock));
        }

        [Fact]
        public async Task GetTopDestinationsAsync_Should_Return_Top10_OrderedByAverageRating()
        {
            // ARRANGE
            // Crear destinos de prueba
            var destination1 = new Destination(Guid.NewGuid(), "Destino A", "País A", "Ciudad A", 1000, "fotoA.jpg", DateTime.Now, new Coordinates(10, 20));
            var destination2 = new Destination(Guid.NewGuid(), "Destino B", "País B", "Ciudad B", 2000, "fotoB.jpg", DateTime.Now, new Coordinates(30, 40));
            var destination3 = new Destination(Guid.NewGuid(), "Destino C", "País C", "Ciudad C", 3000, "fotoC.jpg", DateTime.Now, new Coordinates(50, 60));
            var destination4 = new Destination(Guid.NewGuid(), "Destino D", "País D", "Ciudad D", 4000, "fotoD.jpg", DateTime.Now, new Coordinates(70, 80));
            var destinations = new List<Destination> { destination1, destination2, destination3, destination4 };

            // Crear experiencias (ratings) para los destinos
            var experiences = new List<TravelExperience>
            {
                // Destino A: ratings 5 y 4 → 4.5
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp1", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination1.Id, "Exp2", "Desc", DateTime.Now, 4),
                // Destino B: rating 3 → 3
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination2.Id, "Exp3", "Desc", DateTime.Now, 3),
                // Destino C: ratings 5,5,5 → 5
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp4", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp5", "Desc", DateTime.Now, 5),
                new TravelExperience(Guid.NewGuid(), Guid.NewGuid(), destination3.Id, "Exp6", "Desc", DateTime.Now, 5),
                // Destino D: sin experiencias
            };

            // Configurar mocks para que devuelvan las listas como IQueryable
            _destinationRepoMock.GetQueryableAsync().Returns(destinations.AsQueryable());
            _experienceRepoMock.GetQueryableAsync().Returns(experiences.AsQueryable());

            // ACT
            var result = await _destinationService.GetTopDestinationsAsync();

            // ASSERT
            result.ShouldNotBeNull();
            // Destinos con ratings: A, B, C (D no tiene)
            result.Count.ShouldBe(3);

            // Orden esperado
            // Destino C
            result[0].Id.ShouldBe(destination3.Id);
            result[0].AverageRating.ShouldBe(5.0);

            // Destino A
            result[1].Id.ShouldBe(destination1.Id);
            result[1].AverageRating.ShouldBe(4.5);

            // Destino B
            result[2].Id.ShouldBe(destination2.Id);
            result[2].AverageRating.ShouldBe(3.0);
        }
    }

}