using PibesDelDestino.Users; // Necesario para buscar usuarios
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace PibesDelDestino.Experiences
{
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
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            // 1. FILTRO POR DESTINO (3.4)
            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            // 2. BUSQUEDA POR PALABRAS CLAVE (3.6)
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                // Busca en Título O en Descripción
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            // 3. FILTRO POR VALORACIÓN (3.5)
            if (input.Type.HasValue)
            {
                switch (input.Type.Value)
                {
                    case ExperienceFilterType.Positive:
                        // 4 o 5 estrellas
                        query = query.Where(x => x.Rating >= 4);
                        break;
                    case ExperienceFilterType.Neutral:
                        // 3 estrellas
                        query = query.Where(x => x.Rating == 3);
                        break;
                    case ExperienceFilterType.Negative:
                        // 1 o 2 estrellas
                        query = query.Where(x => x.Rating <= 2);
                        break;
                }
            }

            return query;
        }

        // 2. LÓGICA PARA PONER EL NOMBRE DE USUARIO (JOIN MANUAL)
        protected override async Task<List<TravelExperienceDto>> MapToGetListOutputDtosAsync(List<TravelExperience> entities)
        {
            // Mapeamos lo básico (título, fecha, rating...)
            var dtos = await base.MapToGetListOutputDtosAsync(entities);

            // Recolectamos todos los IDs de usuarios de esta página
            var userIds = entities.Select(x => x.UserId).Distinct().ToArray();

            // Buscamos esos usuarios en la base de datos
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));

            // Asignamos los nombres a los DTOs
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