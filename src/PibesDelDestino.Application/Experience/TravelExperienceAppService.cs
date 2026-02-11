using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Data; // Necesario para GetProperty
using PibesDelDestino.Notifications;

namespace PibesDelDestino.Experiences
{
    [Authorize]
    public class TravelExperienceAppService : CrudAppService<
            TravelExperience,
            TravelExperienceDto,
            Guid,
            GetTravelExperiencesInput,
            CreateUpdateTravelExperienceDto>,
        ITravelExperienceAppService
    {
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly NotificationManager _notificationManager;

        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            NotificationManager notificationManager)
            : base(repository)
        {
            _userRepository = userRepository;
            _notificationManager = notificationManager;
        }

        [AllowAnonymous]
        public async Task<double> GetAverageRatingAsync(Guid destinationId)
        {
            var query = await Repository.GetQueryableAsync();
            var ratings = query.Where(x => x.DestinationId == destinationId);

            if (!await AsyncExecuter.AnyAsync(ratings))
            {
                return 0;
            }

            return await AsyncExecuter.AverageAsync(ratings, x => x.Rating);
        }

        public override async Task<TravelExperienceDto> UpdateAsync(Guid id, CreateUpdateTravelExperienceDto input)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para editar esta reseña.");
            }

            existingExperience.Update(
                input.Title,
                input.Description,
                input.Rating,
                input.Date
            );

            await Repository.UpdateAsync(existingExperience);

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(existingExperience);
        }

        public override async Task DeleteAsync(Guid id)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para eliminar esta reseña.");
            }

            await base.DeleteAsync(id);
        }

        public override async Task<TravelExperienceDto> CreateAsync(CreateUpdateTravelExperienceDto input)
        {
            if (CurrentUser.Id == null)
            {
                throw new AbpAuthorizationException("Debes estar logueado para crear una experiencia.");
            }

            var newExperience = new TravelExperience(
                GuidGenerator.Create(),
                CurrentUser.Id.Value,
                input.DestinationId,
                input.Title,
                input.Description,
                input.Date,
                input.Rating
            );

            await Repository.InsertAsync(newExperience);

            await _notificationManager.NotifyNewCommentAsync(
                input.DestinationId,
                "un destino que seguís",
                CurrentUser.Id.Value
            );

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            if (input.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == input.UserId);
            }

            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            if (input.Type.HasValue)
            {
                switch (input.Type.Value)
                {
                    case ExperienceFilterType.Positive:
                        query = query.Where(x => x.Rating >= 4);
                        break;
                    case ExperienceFilterType.Neutral:
                        query = query.Where(x => x.Rating == 3);
                        break;
                    case ExperienceFilterType.Negative:
                        query = query.Where(x => x.Rating <= 2);
                        break;
                }
            }

            return query;
        }

        // --- 6. MAPEO LISTA CON FOTO 👤 ---
        protected override async Task<List<TravelExperienceDto>> MapToGetListOutputDtosAsync(List<TravelExperience> entities)
        {
            var dtos = await base.MapToGetListOutputDtosAsync(entities);
            var userIds = entities.Select(x => x.UserId).Distinct().ToArray();
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));

            foreach (var dto in dtos)
            {
                var user = users.FirstOrDefault(u => u.Id == dto.UserId);
                if (user != null)
                {
                    dto.UserName = user.UserName;
                    // Mapeamos la URL de la foto desde las ExtraProperties
                    dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
                }
            }

            return dtos;
        }

        // --- 7. MAPEO INDIVIDUAL CON FOTO ---
        protected override async Task<TravelExperienceDto> MapToGetOutputDtoAsync(TravelExperience entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);
            var user = await _userRepository.FindAsync(entity.UserId);
            if (user != null)
            {
                dto.UserName = user.UserName;
                dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
            }
            return dto;
        }

        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }

        [AllowAnonymous]
        public async Task<List<TravelExperienceDto>> GetRecentExperiencesAsync(int count = 5)
        {
            var query = await Repository.GetQueryableAsync();
            var recent = query.OrderByDescending(x => x.CreationTime).Take(count).ToList();
            return await MapToGetListOutputDtosAsync(recent);
        }
    }
}