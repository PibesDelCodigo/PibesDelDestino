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
// 1. IMPORTAMOS LOS NUEVOS NAMESPACES
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

        // 2. DECLARAMOS LOS NUEVOS REPOSITORIOS
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            // 3. INYECTAMOS EN EL CONSTRUCTOR
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<AppNotification, Guid> notificationRepository)
            : base(repository)
        {
            _userRepository = userRepository;
            _favoriteRepository = favoriteRepository;
            _notificationRepository = notificationRepository;
        }

        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }

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

        // 4. MODIFICAMOS EL CREATE PARA DISPARAR NOTIFICACIONES
        public override async Task<TravelExperienceDto> CreateAsync(CreateUpdateTravelExperienceDto input)
        {
            if (CurrentUser.Id == null)
            {
                throw new AbpAuthorizationException("Debes estar logueado para crear una experiencia.");
            }

            // A. Creamos la experiencia normalmente
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

            // -------------------------------------------------------------
            // B. LÓGICA DE NOTIFICACIONES (Requisito 6.1) 🔔
            // -------------------------------------------------------------

            // 1. Buscamos quiénes siguen este destino
            var followers = await _favoriteRepository.GetListAsync(x => x.DestinationId == input.DestinationId);

            // 2. Preparamos la lista de notificaciones
            var notifications = new List<AppNotification>();

            foreach (var follow in followers)
            {
                // No me notifico a mí mismo si comento en algo que sigo
                if (follow.UserId != CurrentUser.Id.Value)
                {
                    notifications.Add(new AppNotification(
                        GuidGenerator.Create(),
                        follow.UserId,
                        "Nuevo Comentario 💬",
                        $"Alguien comentó sobre un destino que sigues: '{input.Title}'",
                        "Comment" // Tipo de notificación
                    ));
                }
            }

            // 3. Guardamos todo junto (InsertMany es más eficiente)
            if (notifications.Any())
            {
                await _notificationRepository.InsertManyAsync(notifications);
            }
            // -------------------------------------------------------------

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

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
    }
}