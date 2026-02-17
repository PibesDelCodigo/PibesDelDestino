using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Data;
using PibesDelDestino.Notifications;
using Microsoft.Extensions.Logging;
using PibesDelDestino.Destinations;

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
        // Inyectamos los servicios necesarios
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly NotificationManager _notificationManager;
        private readonly IRepository<Destination, Guid> _destinationRepository;

        // Constructor para inyectar dependencias
        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<Destination, Guid> destinationRepository,
            NotificationManager notificationManager)
            : base(repository)
        {
            _userRepository = userRepository;
            _notificationManager = notificationManager;
            _destinationRepository = destinationRepository;
        }

        // Método para obtener el rating promedio de un destino

        [AllowAnonymous]
        public async Task<double> GetAverageRatingAsync(Guid destinationId)
        {

            // Obtiene el objeto IQueryable de la entidad. 
            // Esto nos permite construir la consulta SQL dinámicamente (filtros, joins) 
            // antes de ejecutarla contra la base de datos.
            var query = await Repository.GetQueryableAsync();

            //buscamos el destino en especifico
            var ratings = query.Where(x => x.DestinationId == destinationId);

            //Verificamos si hay calificaciones en ese destino. Si no hay, devolvemos 0 para evitar errores al calcular el promedio.
            if (!await AsyncExecuter.AnyAsync(ratings))
            {
                return 0;
            }

            // Si hay calificaciones, usamos AsyncExecuter para la funcion de agregacion y el AverangeAsync para calcular el promedio de las calificaciones.
            return await AsyncExecuter.AverageAsync(ratings, x => x.Rating);
        }

        // Método para crear una nueva experiencia de viaje
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

            // Insertamos la nueva experiencia en el repositorio
            await Repository.InsertAsync(newExperience);

            try
            {

                var destination = await _destinationRepository.GetAsync(input.DestinationId);
                await _notificationManager.NotifyNewCommentAsync(
                    input.DestinationId,
                    destination.Name,
                    CurrentUser.Id.Value
                );
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"No se pudo enviar la notificación: {ex.Message}");
            }

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

        // Método para actualizar una experiencia de viaje
        public override async Task<TravelExperienceDto> UpdateAsync(Guid id, CreateUpdateTravelExperienceDto input)
        {
            var existingExperience = await Repository.GetAsync(id);

            // Verificamos que el usuario actual sea el dueño de la reseña
            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para editar esta reseña.");
            }
            // Actualizamos los campos de la experiencia con los nuevos valores proporcionados en el DTO de entrada
            existingExperience.Update(
                input.Title,
                input.Description,
                input.Rating,
                input.Date
            );

            // Guardamos los cambios en el repositorio
            await Repository.UpdateAsync(existingExperience);

            // Devolvemos el DTO actualizado
            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(existingExperience);
        }

        // Método para eliminar una experiencia de viaje
        public override async Task DeleteAsync(Guid id)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para eliminar esta reseña.");
            }

            await base.DeleteAsync(id);
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

        //Este método se encarga de mapear la lista de entidades de experiencias de viaje a una lista de DTOs, incluyendo la información del usuario (nombre y foto) para cada experiencia.
        protected override async Task<List<TravelExperienceDto>> MapToGetListOutputDtosAsync(List<TravelExperience> entities)
        {
           
            var dtos = await base.MapToGetListOutputDtosAsync(entities);
            return await EnrichDtosWithUserData(dtos);
        }

        //Este método se encarga de mapear una entidad de experiencia de viaje a un DTO, incluyendo la información del usuario (nombre y foto) para esa experiencia específica.
        protected override async Task<TravelExperienceDto> MapToGetOutputDtoAsync(TravelExperience entity)
        {
            var dto = await base.MapToGetOutputDtoAsync(entity);
            // Reutilizamos la lógica de lista para un solo elemento
            var list = await EnrichDtosWithUserData(new List<TravelExperienceDto> { dto });
            return list.First();
        }

        private async Task<List<TravelExperienceDto>> EnrichDtosWithUserData(List<TravelExperienceDto> dtos)
        {
            if (!dtos.Any()) return dtos;

            var userIds = dtos.Select(x => x.UserId).Distinct().ToArray();

            // Traemos todos los usuarios necesarios en una sola consulta (evita N+1)
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));

            foreach (var dto in dtos)
            {
                var user = users.FirstOrDefault(u => u.Id == dto.UserId);
                if (user != null)
                {
                    dto.UserName = user.UserName;
                    // Usamos GetProperty de forma segura para data extra
                    dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
                }
            }
            return dtos;
        }

        // Este método se encarga de obtener las experiencias de viaje más recientes, ordenándolas por fecha de creación y limitando el número de resultados según el parámetro "count".
        // Se marca con [AllowAnonymous] para permitir el acceso sin autenticación, ya que esta información puede ser útil para mostrar en la página principal o en secciones destacadas de la aplicación.
        [AllowAnonymous]
        public async Task<List<TravelExperienceDto>> GetRecentExperiencesAsync(int count = 5)
        {
            var query = await Repository.GetQueryableAsync();

            // Ejecutamos la query ordenada y paginada en base de datos
            var recentEntities = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.CreationTime).Take(count)
            );

            return await MapToGetListOutputDtosAsync(recentEntities);
        }

        // Permitimos que cualquiera vea la lista de reseñas (sin estar logueado)
        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }

        // Permitimos que cualquiera vea el detalle de una reseña específica
        [AllowAnonymous]
        public override async Task<TravelExperienceDto> GetAsync(Guid id)
        {
            return await base.GetAsync(id);
        }
    }
}

