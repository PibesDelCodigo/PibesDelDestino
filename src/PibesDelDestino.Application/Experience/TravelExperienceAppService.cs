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
using Volo.Abp.Data; // Necesario para leer .GetProperty
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

        // --- 1. PROMEDIO DE ESTRELLAS ⭐ ---
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

        // --- 2. UPDATE SEGURO ✅ ---
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

        // --- 3. DELETE SEGURO 🗑️ ---
        public override async Task DeleteAsync(Guid id)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para eliminar esta reseña.");
            }

            await base.DeleteAsync(id);
        }

        // --- 4. CREATE CON NOTIFICACIONES INTELIGENTES 🔔 ---
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

            // --- LÓGICA DE NOTIFICACIONES FILTRADA ---

            // 1. Buscar seguidores del destino
            var followers = await _favoriteRepository.GetListAsync(x => x.DestinationId == input.DestinationId);

            // 2. Obtener los IDs de usuario únicos
            var followerUserIds = followers.Select(f => f.UserId).Distinct().ToList();

            // 3. Traer los usuarios de la base de datos para chequear sus preferencias
            var usersToNotify = await _userRepository.GetListAsync(u => followerUserIds.Contains(u.Id));

            var notifications = new List<AppNotification>();

            foreach (var user in usersToNotify)
            {
                // A. No notificarse a uno mismo
                if (user.Id == CurrentUser.Id.Value) continue;

                // B. Verificar el Switch de Configuración
                // Si es nulo (nunca tocó la config), asumimos true. Si es false, no enviamos.
                var wantsNotifications = user.GetProperty<bool?>("ReceiveNotifications") ?? true;

                if (wantsNotifications)
                {
                    notifications.Add(new AppNotification(
                        GuidGenerator.Create(),
                        user.Id,
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

        // --- 5. FILTROS AVANZADOS 🔍 ---
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            // Filtro por Destino
            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            // Filtro por Usuario (Perfil Público)
            if (input.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == input.UserId);
            }

            // Filtro por Texto
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            // Filtro por Tipo (Positiva/Negativa)
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

        // --- 6. MAPEO LISTA 👤 ---
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

        // --- 7. MAPEO INDIVIDUAL ---
        protected override async Task<TravelExperienceDto> MapToGetOutputDtoAsync(TravelExperience entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);
            var user = await _userRepository.FindAsync(entity.UserId);
            if (user != null)
            {
                dto.UserName = user.UserName;
            }
            return dto;
        }

        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }
    }
}