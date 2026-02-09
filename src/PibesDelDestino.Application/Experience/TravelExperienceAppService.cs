// REQUERIMIENTOS 3.1, 3.2 y 3.3: Gestión de Experiencias (CRUD).
// Permite a los usuarios crear reseñas sobre un destino, editarlas si son propias,
// o eliminarlas. Se valida la autoría mediante las políticas de ABP.

//REQUERIMIENTOS 3.4, 3.5 y 3.6: Consulta y Filtros.
// Recupera las experiencias de un destino paginadas.
// Implementa la lógica de filtrado por:
// - Valoración (Positiva/Negativa/Neutral).
// - Palabras clave (búsqueda de texto en el contenido).

// REQUERIMIENTOS 6.1 y 6.3: Notificaciones Inteligentes
// 6.1: Detecta seguidores del destino y crea una alerta de nuevo comentario.
// 6.3: Respeta la preferencia del usuario ("ReceiveNotifications") antes de enviar.
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
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;

namespace PibesDelDestino.Experiences
{//nadie puede leer, crear, o editar una experiencia sin estar logueado.
    [Authorize]
    public class TravelExperienceAppService : CrudAppService<
            TravelExperience,
            TravelExperienceDto,
            Guid,
            GetTravelExperiencesInput,
            CreateUpdateTravelExperienceDto>,
        ITravelExperienceAppService
    {//user repository para saber quien escribio la reseña.
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

        // --- 1. PROMEDIO DE ESTRELLAS  ---
       //puerta abierta, entonces para escribir tenes que estar logueado pero para leer no
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
        } //todo este metodo devuelve el promedio pero sin traer la informacion pesada
          //como las fotos, descripciones, etc. Es util para actualizacion ligeras de UI

        // REQ 3.2. UPDATE SEGURO  ---
        public override async Task<TravelExperienceDto> UpdateAsync(Guid id, CreateUpdateTravelExperienceDto input)
        {
            var existingExperience = await Repository.GetAsync(id);
            // REQ 4.3 - Verificar autoría
            if (existingExperience.UserId != CurrentUser.Id)
            {
//esta validacion se hace porque el update que tiene abp te dejaria editar o entrar a una 
//reseña solo con un permiso de travelExperience.edit, en cambio al hacer esta validacion
//obligas a que se verifique que el id del que quiere editar la experiencia sea el mismo del que la creó.
   //No basta con el permiso "editar", hay que ser propietario del registro.
                throw new AbpAuthorizationException("No tienes permiso para editar esta reseña.");
            }
            //no modificamos las propiedades desde fuera cambiando tipo input.title, sino que le pedimos
            //a la entidad que se actualice a si misma a través de un metodo de dominio.
            existingExperience.Update(
                input.Title,
                input.Description,
                input.Rating,
                input.Date
            );
//convertis la entidad actualizada en DTO para devolverlo al front
            await Repository.UpdateAsync(existingExperience);

            return ObjectMapper.Map<TravelExperience, TravelExperienceDto>(existingExperience);
        }

        // REQ 3.3. DELETE SEGURO  ---
        public override async Task DeleteAsync(Guid id)
        {
            var existingExperience = await Repository.GetAsync(id);

            if (existingExperience.UserId != CurrentUser.Id)
            {
                throw new AbpAuthorizationException("No tienes permiso para eliminar esta reseña.");
            }

            await base.DeleteAsync(id);
        }

        // REQ 3.1 crear experiencia  ---
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

            // 3. Traer los usuarios de la base de datos si es que tienen activada
            //las notificaciones
            var usersToNotify = await _userRepository.GetListAsync(u => followerUserIds.Contains(u.Id));

            var notifications = new List<AppNotification>();

            foreach (var user in usersToNotify)
            {
                // A. No notificarse a uno mismo
                if (user.Id == CurrentUser.Id.Value) continue;

                // B. Verificar el Switch de Configuración
                // Si es nulo (nunca tocó la config), asumimos true. Si es false, no enviamos.
                //REQ 6.1              
                var wantsNotifications = user.GetProperty<bool?>("ReceiveNotifications") ?? true;

                //REQ 6.3 el filtro solo si quiere recibir notificaciones
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

        // --- 5. FILTROS AVANZADOS ---
        //Ejemplo pizzeria, "iqueryable" me ayuda porque puedo meter los filtros antes
        //de un getlistAsync y asi evitar pedir datos que no necesito.
        protected override async Task<IQueryable<TravelExperience>> CreateFilteredQueryAsync(GetTravelExperiencesInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            // REQ 3.4 Filtro por Destino
            if (input.DestinationId.HasValue)
            {
                query = query.Where(x => x.DestinationId == input.DestinationId);
            }

            // REQ 3.4 Filtro por Usuario (Perfil Público)
            if (input.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == input.UserId);
            }

            // REQ 3.6 Filtro por Texto
            //Haces dos busquedas en paralelo, buscando la similitud con el titulo
            //o con la descripcion, validando primero que haya escrito algo util.
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                query = query.Where(x => x.Title.Contains(input.FilterText) ||
                                         x.Description.Contains(input.FilterText));
            }

            // REQ 3.5 Filtro por Tipo (Positiva/Negativa)
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

        // --- 6. MAPEO LISTA  ---
        protected override async Task<List<TravelExperienceDto>> MapToGetListOutputDtosAsync(List<TravelExperience> entities)
        {//dtos son experiencias, traemos desde la base de datos las enntities
        //en UserIds, guardamos los ids de quienes hicieron las experiencias,
        //sin repetir el usuario.
        //En users traemos la info de los usuarios desde la base de datos.
            var dtos = await base.MapToGetListOutputDtosAsync(entities);
            var userIds = entities.Select(x => x.UserId).Distinct().ToArray();
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));
            //Por cada experiencia, buscas entre los usuarios cual es el usuario que realizo
            //la experiencia y le asignas el nombre de usuario al DTO y si el usuario es 
            //null (por alguna razon no lo encuentra) entonces no le asignas nada.
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
        //"Al igual que en el listado, necesitamos enriquecer el DTO con el nombre
        //del usuario.
        //Como en este caso estamos procesando una única entidad, utilizamos
        //FindAsync directamente, ya que hacer una única consulta extra a la
        //base de datos no impacta negativamente en el rendimiento
        
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

        //REQ 4.5 - Permitir lista pública de experiencias
        //decidimos que las experiencias sean públicas ([AllowAnonymous]).
        //Sobrescribimos el método GetListAsync únicamente para cambiar su
        //nivel de acceso. Esto permite que usuarios no registrados puedan
        //leer las reseñas y usar los filtros,
        [AllowAnonymous]
        public override async Task<PagedResultDto<TravelExperienceDto>> GetListAsync(GetTravelExperiencesInput input)
        {
            return await base.GetListAsync(input);
        }
    }
}