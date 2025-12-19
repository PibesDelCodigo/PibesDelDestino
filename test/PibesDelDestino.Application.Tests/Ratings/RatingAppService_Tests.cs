using System;
using System.Threading.Tasks;
using PibesDelDestino.Destinations;
using PibesDelDestino.Ratings;
using Shouldly;
using Xunit;
using PibesDelDestino;
using Volo.Abp.Domain.Repositories;
using NSubstitute;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace PibesDelDestino.Ratings
{
    public class RatingAppService_Tests : PibesDelDestinoApplicationTestBase<PibesDelDestinoApplicationTestModule>
    {
        private readonly IRatingAppService _ratingAppService;
        private readonly IRepository<Rating, Guid> _ratingRepositoryMock;

        public RatingAppService_Tests()
        {
            // Creamos un Mock del repositorio que fallaba
            _ratingRepositoryMock = Substitute.For<IRepository<Rating, Guid>>();

            // Obtenemos los servicios reales que SÍ funcionan
            var guidGenerator = GetRequiredService<IGuidGenerator>();
            var objectMapper = GetRequiredService<IObjectMapper>();
            var currentUser = GetRequiredService<ICurrentUser>();

            // Instanciamos el servicio manualmente pasándole el Mock que faltaba
            _ratingAppService = new RatingAppService(
                _ratingRepositoryMock,
                guidGenerator,
                objectMapper,
                currentUser
            );
        }

        [Fact]
        public async Task CreateAsync_Should_Create_Rating_Correctly()
        {
            var input = new CreateRatingDto
            {
                DestinationId = Guid.NewGuid(),
                Score = 5,
                Comment = "Test exitoso"
            };

            var result = await _ratingAppService.CreateAsync(input);

            result.ShouldNotBeNull();
            // Verificamos que el repositorio recibió la orden de insertar
            await _ratingRepositoryMock.Received(1).InsertAsync(Arg.Any<Rating>());
        }
    }
}