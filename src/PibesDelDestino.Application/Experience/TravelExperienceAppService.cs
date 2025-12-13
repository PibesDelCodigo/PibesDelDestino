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

namespace PibesDelDestino.Experiences
{
    // 🔒 1. LA CLASE ESTÁ PROTEGIDA POR DEFECTO (Nadie entra sin login)
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

        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository)
            : base(repository)
        {
            _userRepository = userRepository;
        }

        // 🔓 2. EXCEPCIÓN: ABRIMOS LA LECTURA PARA TODOS
        // Esto permite que usuarios anónimos vean la lista de reseñas
        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }

        // 3. LOGICA DE FILTROS (Destino, Texto, Estrellas)
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            // Filtro por Destino
            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            // Búsqueda por Texto
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            // Filtro por Valoración
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

        // 4. CREACIÓN SEGURA (Asigna el usuario logueado)
        public override async Task<TravelExperienceDto> CreateAsync(CreateUpdateTravelExperienceDto input)
        {
            if (CurrentUser.Id == null)
            {
                throw new AbpAuthorizationException("Debes estar logueado para crear una experiencia.");
            }

            var newExperience = new TravelExperience(
                GuidGenerator.Create(),
                CurrentUser.Id.Value, // Asignamos el ID real
                input.DestinationId,
                input.Title,
                input.Description,
                input.Date,
                input.Rating
            );

            await Repository.InsertAsync(newExperience);

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

        // 5. MAPE DE NOMBRES DE USUARIO
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