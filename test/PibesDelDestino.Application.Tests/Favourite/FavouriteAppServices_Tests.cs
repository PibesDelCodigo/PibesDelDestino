using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using PibesDelDestino.Favorites;
using PibesDelDestino.Destinations;
using Volo.Abp.Users;
using Volo.Abp.Guids;

namespace PibesDelDestino.Favorites
{
    public class FavoriteAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly IFavoriteAppService _favoriteAppService;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepositoryMock;
        private readonly IRepository<Destination, Guid> _destinationRepositoryMock;

        public FavoriteAppService_Tests()
        {
            _favoriteRepositoryMock = Substitute.For<IRepository<FavoriteDestination, Guid>>();
            _destinationRepositoryMock = Substitute.For<IRepository<Destination, Guid>>();

            _favoriteAppService = new FavoriteAppServiceTestProxy(
                _favoriteRepositoryMock,
                _destinationRepositoryMock,
                GetRequiredService<IServiceProvider>()
            );
        }

        [Fact]
        public async Task Should_Toggle_Favorite_Status()
        {
            var destinationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var currentUser = GetRequiredService<ICurrentUser>();

            var destination = new Destination(
                destinationId,
                "Tokyo",
                "Japan",
                "Tokyo City",
                14000000,
                "photo.jpg",
                DateTime.Now,
                new Coordinates(35.6762f, 139.6503f)
            );

            //Simulamos que no existe en favoritos aun
            _favoriteRepositoryMock.FindAsync(Arg.Any<Expression<Func<FavoriteDestination, bool>>>())
                .Returns(Task.FromResult<FavoriteDestination>(null));

            var input = new CreateFavoriteDto { DestinationId = destinationId };
            await _favoriteAppService.ToggleAsync(input);

            // Verificamos que al no existir, se llamó al Insert
            await _favoriteRepositoryMock.Received(1).InsertAsync(Arg.Any<FavoriteDestination>());
        }
    }

    public class FavoriteAppServiceTestProxy : FavoriteAppService
    {
        public FavoriteAppServiceTestProxy(
            IRepository<FavoriteDestination, Guid> repository,
            IRepository<Destination, Guid> destinationRepository,
            IServiceProvider serviceProvider)
            : base(repository, destinationRepository)
        {
            LazyServiceProvider = serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
            var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        }
    }
}