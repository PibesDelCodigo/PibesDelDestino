using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace PibesDelDestino.Ratings
{
    // Este servicio se encarga de manejar las operaciones relacionadas con las calificaciones
    // de los destinos por parte de los usuarios.
    [Authorize]
    public class RatingAppService : ApplicationService, IRatingAppService
    {
        private readonly IRepository<Rating, Guid> _ratingRepository;
        private readonly IGuidGenerator _guidGenerator;

        // El IObjectMapper se utiliza para mapear entre las entidades de dominio y los DTOs,
        // lo que facilita la conversión de datos entre diferentes capas de la aplicación.

        private readonly IObjectMapper _objectMapper;
        private readonly ICurrentUser _currentUser; 

        public RatingAppService(
            IRepository<Rating, Guid> ratingRepository,
            IGuidGenerator guidGenerator,
            IObjectMapper objectMapper,
            ICurrentUser currentUser) 
        {
            _ratingRepository = ratingRepository;
            _guidGenerator = guidGenerator;
            _objectMapper = objectMapper;
            _currentUser = currentUser;
        }

        // Este método se encarga de crear una nueva calificación para un destino específico. 
        public async Task<RatingDto> CreateAsync(CreateRatingDto input)
        {
            var rating = new Rating(
                _guidGenerator.Create(),
                input.DestinationId,
                _currentUser.GetId(),
                input.Score,
                input.Comment
            );

            await _ratingRepository.InsertAsync(rating);

            return _objectMapper.Map<Rating, RatingDto>(rating);
        }
    }
}