using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using PibesDelDestino.Destinations;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;

namespace PibesDelDestino.Notifications
{
    public class NotificationManager : DomainService
    {
        // CAMBIO 1: Usamos AppNotification (el nombre real de tu clase)
        private readonly IRepository<AppNotification, Guid> _notificationRepository;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<Destination, Guid> _destinationRepository;

        public NotificationManager(
            IRepository<AppNotification, Guid> notificationRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<Destination, Guid> destinationRepository)
        {
            _notificationRepository = notificationRepository;
            _favoriteRepository = favoriteRepository;
            _destinationRepository = destinationRepository;
        }

        // CASO 1: CAMBIO EN DESTINO
        public async Task NotifyDestinationUpdateAsync(Destination destination, string changeDescription)
        {
            var title = $"Actualización en {destination.Name}";
            var message = $"Hubo cambios recientes: {changeDescription}. ¡Echale un vistazo!";

            await CreateNotificationsForDestinationAsync(destination.Id, title, message, NotificationType.DestinationUpdate);
        }

        // CASO 2: NUEVO COMENTARIO
        public async Task NotifyNewCommentAsync(Guid destinationId, string destinationName, Guid commenterId)
        {
            var title = $"Nuevo comentario en {destinationName}";
            var message = "Alguien ha dejado una nueva opinión en un destino que seguís.";

            await CreateNotificationsForDestinationAsync(destinationId, title, message, NotificationType.Comment, excludeUserId: commenterId);
        }

        // CASO 3: EVENTO TICKETMASTER
        public async Task NotifyEventInCityAsync(string cityName, string eventName, string eventUrl)
        {
            var destinationsInCity = await _destinationRepository.GetListAsync(d => d.City == cityName);

            if (!destinationsInCity.Any()) return;

            var destinationIds = destinationsInCity.Select(d => d.Id).ToList();

            // OJO ACÁ: Usamos DestinationId (singular) porque así es tu entidad FavoriteDestination
            var interestedFavorites = await _favoriteRepository.GetListAsync(f => destinationIds.Contains(f.DestinationId));

            var uniqueUserIds = interestedFavorites.Select(f => f.UserId).Distinct();

            foreach (var userId in uniqueUserIds)
            {
                // CAMBIO 2: NewAppNotification + .ToString()
                await _notificationRepository.InsertAsync(new AppNotification(
                    GuidGenerator.Create(),
                    userId,
                    $"¡Evento en {cityName}!",
                    $"{eventName} está por ocurrir. ¡No te lo pierdas!",
                    NotificationType.NewEvent.ToString() // <--- SOLUCIÓN: Convertimos a string
                ));
            }
        }

        // --- MÉTODO PRIVADO ---
        private async Task CreateNotificationsForDestinationAsync(
            Guid destinationId,
            string title,
            string message,
            NotificationType type,
            Guid? excludeUserId = null)
        {
            var favorites = await _favoriteRepository.GetListAsync(f => f.DestinationId == destinationId);

            foreach (var fav in favorites)
            {
                if (excludeUserId.HasValue && fav.UserId == excludeUserId.Value) continue;

                // CAMBIO 3: NewAppNotification + .ToString()
                await _notificationRepository.InsertAsync(new AppNotification(
                    GuidGenerator.Create(),
                    fav.UserId,
                    title,
                    message,
                    type.ToString() // <--- SOLUCIÓN: Convertimos a string
                ));
            }
        }
    }
}