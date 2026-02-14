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
using Volo.Abp.Data;
using PibesDelDestino.Notifications;
using Microsoft.Extensions.Logging;

namespace PibesDelDestino.Experiences
{
    //Heredamos de CrudAppService, ya que nos da las operaciones basicas de CRUD
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

        // Constructor para inyectar dependencias
        public TravelExperienceAppService(
            IRepository<TravelExperience, Guid> repository,
            IRepository<IdentityUser, Guid> userRepository,
            NotificationManager notificationManager)
            : base(repository)
        {
            _userRepository = userRepository;
            _notificationManager = notificationManager;
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
                var destinationRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<PibesDelDestino.Destinations.Destination, Guid>>();
                var destination = await destinationRepo.GetAsync(input.DestinationId);

                // Notificamos a los seguidores del destino sobre la nueva experiencia
                await _notificationManager.NotifyNewCommentAsync(
                    input.DestinationId,
                    destination.Name,
                    CurrentUser.Id.Value
                );
            }
            catch (Exception ex)
            {

                Logger.LogWarning("No se pudo enviar la notificacion del comentario: " + ex.Message);
            }

            // Devolvemos el DTO de la nueva experiencia creada
            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(newExperience);
        }

        //Este metodo se encarga de construir la consulta para obtener las experiencias de viaje, aplicando los filtros que el usuario haya especificado en el input.
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            // Obtenemos el IQueryable base de la entidad, que nos permite construir la consulta con los filtros necesarios.
            var query = await base.CreateFilteredQueryAsync(input);

            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            if (input.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == input.UserId);
            }

            // Si el usuario ha proporcionado un texto de filtro, aplicamos un filtro que busque ese texto en el título o la descripción de las experiencias.
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            // Si el usuario ha especificado un tipo de experiencia (positiva, neutral, negativa), aplicamos un filtro basado en la calificación (rating) de las experiencias.
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
            // Finalmente, devolvemos el IQueryable con todos los filtros aplicados. Este IQueryable se ejecutará posteriormente para obtener los datos de la base de datos.
            return query;
        }

        //Este método se encarga de mapear la lista de entidades de experiencias de viaje a una lista de DTOs, incluyendo la información del usuario (nombre y foto) para cada experiencia.
        protected override async Task<List<TravelExperienceDto>> MapToGetListOutputDtosAsync(List<TravelExperience> entities)
        {
           
            var dtos = await base.MapToGetListOutputDtosAsync(entities);
            var userIds = entities.Select(x => x.UserId).Distinct().ToArray();
            // Obtenemos la lista de usuarios correspondientes a los UserIds de las experiencias, para luego mapear su información (nombre y foto) en los DTOs.
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));

            foreach (var dto in dtos)
            {
                // Para cada DTO de experiencia, buscamos el usuario correspondiente en la lista de usuarios obtenida previamente, y si lo encontramos, mapeamos su nombre y foto en el DTO.
                var user = users.FirstOrDefault(u => u.Id == dto.UserId);
                if (user != null)
                {
                    dto.UserName = user.UserName;
                    dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
                }
            }

            return dtos;
        }

        //Este método se encarga de mapear una entidad de experiencia de viaje a un DTO, incluyendo la información del usuario (nombre y foto) para esa experiencia específica.
        protected override async Task<TravelExperienceDto> MapToGetOutputDtoAsync(TravelExperience entity)
        {
            // Primero, mapeamos la entidad a un DTO utilizando el método base, que se encarga de mapear los campos básicos de la experiencia.
            var dto = await base.MapToGetOutputDtoAsync(entity);
            var user = await _userRepository.FindAsync(entity.UserId);
            if (user != null)
            {
                dto.UserName = user.UserName;
                dto.UserProfilePicture = user.GetProperty<string>("ProfilePictureUrl");
            }
            return dto;
        }


        // Este método se encarga de obtener la lista paginada de experiencias de viaje, aplicando los filtros y mapeando la información del usuario para cada experiencia. Se marca con [AllowAnonymous] para permitir el acceso sin autenticación.
        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }

        // Este método se encarga de obtener las experiencias de viaje más recientes, ordenándolas por fecha de creación y limitando el número de resultados según el parámetro "count".
        // Se marca con [AllowAnonymous] para permitir el acceso sin autenticación, ya que esta información puede ser útil para mostrar en la página principal o en secciones destacadas de la aplicación.
        [AllowAnonymous]
        public async Task<List<TravelExperienceDto>> GetRecentExperiencesAsync(int count = 5)
        { 
            var query = await Repository.GetQueryableAsync();
            var recent = query.OrderByDescending(x => x.CreationTime).Take(count).ToList();
            return await MapToGetListOutputDtosAsync(recent);
        }
    }
}