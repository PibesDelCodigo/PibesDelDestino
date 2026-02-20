using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Data;

namespace PibesDelDestino.Ratings
{
    [Authorize]
    public class RatingAppService : ApplicationService, IRatingAppService
    {
        private readonly IRepository<Rating, Guid> _ratingRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public RatingAppService(
            IRepository<Rating, Guid> ratingRepository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _ratingRepository = ratingRepository;
            _userRepository = userRepository;
        }

        public async Task<RatingDto> CreateAsync(CreateRatingDto input)
        {
            // 1. EL ESCUDO: Verificamos duplicados
            var existingRating = await _ratingRepository.FindAsync(x =>
                x.DestinationId == input.DestinationId &&
                x.UserId == CurrentUser.Id
            );

            if (existingRating != null)
            {
                throw new Volo.Abp.UserFriendlyException("Ya hiciste una reseña para este destino.");
            }

            // 2. CREACIÓN PURA
            var newRating = new Rating(
                GuidGenerator.Create(),
                input.DestinationId,
                CurrentUser.Id.Value,
                input.Score,
                input.Comment
            );

            await _ratingRepository.InsertAsync(newRating);

            return await MapToDtoWithUserInfo(newRating);
        }

        // EL MÉTODO PARA LOS 3 PUNTITOS
        public async Task<RatingDto> UpdateAsync(Guid id, CreateRatingDto input)
        {
            var rating = await _ratingRepository.GetAsync(id);

            if (rating.UserId != CurrentUser.Id)
            {
                throw new Volo.Abp.UserFriendlyException("No podés editar esto.");
            }

            rating.Update(input.Score, input.Comment);
            await _ratingRepository.UpdateAsync(rating);

            return await MapToDtoWithUserInfo(rating);
        }

        // =================================================================================
        // OBTENER MI CALIFICACIÓN (Para mostrar mis estrellas en la UI)
        // =================================================================================
        public async Task<RatingDto> GetMyRatingAsync(Guid destinationId)
        {
            var myRating = await _ratingRepository.FindAsync(x =>
                x.DestinationId == destinationId &&
                x.UserId == CurrentUser.Id
            );

            if (myRating == null)
            {
                return null; // El usuario aún no ha votado este lugar
            }

            return await MapToDtoWithUserInfo(myRating);
        }

        // =================================================================================
        // MÉTODO PRIVADO (Helper para llenar datos de usuario)
        // =================================================================================
        private async Task<RatingDto> MapToDtoWithUserInfo(Rating rating)
        {
            // Mapeo base (Entidad -> DTO)
            var dto = ObjectMapper.Map<Rating, RatingDto>(rating);

            // Buscamos al usuario autor de la reseña
            var user = await _userRepository.FindAsync(rating.UserId);

            if (user != null)
            {
                dto.UserName = user.UserName;
                // Obtenemos la foto (propiedad extra en ABP)
                dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
            }

            return dto;
        }
    }
}