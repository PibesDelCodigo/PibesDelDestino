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

        // =================================================================================
        // CREAR O ACTUALIZAR (UPSERT)
        // =================================================================================
        public async Task<RatingDto> CreateOrUpdateAsync(CreateRatingDto input)
        {
            // 1. Buscamos si ya existe una calificación de ESTE usuario para ESTE destino
            var existingRating = await _ratingRepository.FindAsync(x =>
                x.DestinationId == input.DestinationId &&
                x.UserId == CurrentUser.Id
            );

            Rating ratingToReturn;

            if (existingRating != null)
            {
                // CASO A: El usuario ya votó, quiere cambiar su opinión.
                // Usamos el método DDD 'Update' que creamos en la Entidad.
                existingRating.Update(input.Score, input.Comment);

                await _ratingRepository.UpdateAsync(existingRating);
                ratingToReturn = existingRating;
            }
            else
            {
                // CASO B: Es la primera vez que vota.
                var newRating = new Rating(
                    GuidGenerator.Create(),
                    input.DestinationId,
                    CurrentUser.Id.Value,
                    input.Score,
                    input.Comment
                );

                await _ratingRepository.InsertAsync(newRating);
                ratingToReturn = newRating;
            }

            // Devolvemos el DTO enriquecido con nombre y foto
            return await MapToDtoWithUserInfo(ratingToReturn);
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