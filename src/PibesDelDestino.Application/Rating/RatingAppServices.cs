using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users; // <-- Asegúrate de tener este using

namespace PibesDelDestino.Ratings
{
    [Authorize]
    public class RatingAppService : ApplicationService, IRatingAppService
    {
        private readonly IRepository<Rating, Guid> _ratingRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IObjectMapper _objectMapper;
        private readonly ICurrentUser _currentUser; // <-- 1. Variable privada

        public RatingAppService(
            IRepository<Rating, Guid> ratingRepository,
            IGuidGenerator guidGenerator,
            IObjectMapper objectMapper,
            ICurrentUser currentUser) // <-- 2. Lo pedimos en el constructor
        {
            _ratingRepository = ratingRepository;
            _guidGenerator = guidGenerator;
            _objectMapper = objectMapper;
            _currentUser = currentUser; // <-- 3. Lo guardamos
        }

        public async Task<RatingDto> CreateAsync(CreateRatingDto input)
        {
            var rating = new Rating(
                _guidGenerator.Create(),
                input.DestinationId,
                _currentUser.GetId(), // <-- 4. Usamos nuestra variable
                input.Score,
                input.Comment
            );

            await _ratingRepository.InsertAsync(rating);

            return _objectMapper.Map<Rating, RatingDto>(rating);
        }
    }
}