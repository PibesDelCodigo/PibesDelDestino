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
using PibesDelDestino.Favorites;
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
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository)
            : base(repository)
        {
            _userRepository = userRepository;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
        }

        // --- 1. PROMEDIO DE ESTRELLAS (Nuevo) ⭐ ---
        [AllowAnonymous]
        public async Task<double> GetAverageRatingAsync(Guid destinationId)
        {
            var query = await Repository.GetQueryableAsync();
            var ratings = query.Where(x => x.DestinationId == destinationId);

            // Evitamos división por cero si no hay votos
            if (!await AsyncExecuter.AnyAsync(ratings))
            {
                return 0;
            }

            return await AsyncExecuter.AverageAsync(ratings, x => x.Rating);
        }

        // --- 2. UPDATE SEGURO (Usando el método de la Entidad) ✅ ---
        public override async Task<TravelExperienceDto> UpdateAsync(Guid id, CreateUpdateTravelExperienceDto input)
        {
            var existingExperience = await Repository.GetAsync(id);

            // Validación de seguridad: Solo el dueño edita
            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para editar esta reseña.");
            }

            // ACÁ ESTÁ LA MAGIA DDD:
            // En vez de asignar propiedades una por una (que daría error por private set),
            // le pedimos a la entidad que se actualice a sí misma.
            existingExperience.Update(
                input.Title,
                input.Description,
                input.Rating,
                input.Date
            );

            await Repository.UpdateAsync(existingExperience);

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(existingExperience);
        }

        // --- 3. DELETE SEGURO (Solo el dueño borra) 🗑️ ---
        public override async Task DeleteAsync(Guid id)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para eliminar esta reseña.");
            }

            await base.DeleteAsync(id);
        }

        // --- 4. CREATE CON NOTIFICACIONES (Tu código original) 🔔 ---
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

            // Lógica de Notificaciones
            var followers = await _favoriteRepository.GetListAsync(x => x.DestinationId == input.DestinationId);
            var notifications = new List<AppNotification>();

            foreach (var follow in followers)
            {
                if (follow.UserId != CurrentUser.Id.Value)
                {
                    notifications.Add(new AppNotification(
                        GuidGenerator.Create(),
                        follow.UserId,
                        "Nuevo Comentario 💬",
                        $"Alguien comentó sobre un destino que sigues: '{input.Title}'",
                        "Comment"
                    ));
                }
            }

            if (notifications.Any())
            {
                await _notificationRepository.InsertManyAsync(notifications);
            }

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

        // --- 5. FILTROS AVANZADOS (Tu código original) 🔍 ---
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
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

        // --- 6. MAPEO DE NOMBRES DE USUARIO 👤 ---
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
                }
            }

            return dtos;
        }

        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }
    }
}