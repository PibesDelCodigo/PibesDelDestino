using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;
using Volo.Abp.Emailing;
using Volo.Abp.Data;
using Microsoft.Extensions.Logging;
using PibesDelDestino.Destinations;
using PibesDelDestino.Favorites;

namespace PibesDelDestino.Notifications
{
    public class NotificationManager : DomainService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository;
        private readonly IRepository<FavoriteDestination, Guid> _favoriteRepository;
        private readonly IRepository<Destination, Guid> _destinationRepository;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationManager> _logger;

        public NotificationManager(
            IRepository<AppNotification, Guid> notificationRepository,
            IRepository<FavoriteDestination, Guid> favoriteRepository,
            IRepository<Destination, Guid> destinationRepository,
            IIdentityUserRepository userRepository,
            IEmailSender emailSender,
            ILogger<NotificationManager> logger)
        {
            _notificationRepository = notificationRepository;
            _favoriteRepository = favoriteRepository;
            _destinationRepository = destinationRepository;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task NotifyEventInCityAsync(string cityName, string eventName, string eventUrl)
        {
            //Buscamos destinos en esa ciudad
            var destinationsInCity = await _destinationRepository.GetListAsync(d => d.City == cityName);
            if (!destinationsInCity.Any()) return;

            var destinationIds = destinationsInCity.Select(d => d.Id).ToList();

            //Buscamos quién tiene esos destinos en favoritos
            var interestedFavorites = await _favoriteRepository.GetListAsync(f => destinationIds.Contains(f.DestinationId));

            // Obtenemos IDs únicos de usuarios
            var uniqueUserIds = interestedFavorites.Select(f => f.UserId).Distinct().ToList();

            _logger.LogInformation($"📢 Notificando evento '{eventName}' en {cityName} a {uniqueUserIds.Count} usuarios.");

            //Notificamos a cada uno
            foreach (var userId in uniqueUserIds)
            {
                await NotifyUserWithPreferencesAsync(
                    userId,
                    $"¡Evento en {cityName}!",
                    $"{eventName} está por ocurrir. <a href='{eventUrl}'>Ver entradas</a>.",
                    $"{eventName} está por ocurrir.",
                    NotificationType.NewEvent
                );
            }
        }

        public async Task NotifyDestinationUpdateAsync(Destination destination, string changeDescription)
        {
            var title = $"Actualización en {destination.Name}";
            var message = $"Hubo cambios recientes: {changeDescription}. ¡Echale un vistazo!";
            await ProcessDestinationNotificationsAsync(destination.Id, title, message, NotificationType.DestinationUpdate);
        }

        public async Task NotifyNewCommentAsync(Guid destinationId, string destinationName, Guid commenterId)
        {
            var title = $"Nuevo comentario en {destinationName}";
            var message = $"¡Buenas noticias! Un viajero ha compartido una nueva experiencia en <b>{destinationName}</b>. Entra a la app para leer qué le pareció.";
            await ProcessDestinationNotificationsAsync(destinationId, title, message, NotificationType.Comment, excludeUserId: commenterId);
        }

        private async Task ProcessDestinationNotificationsAsync(Guid destinationId, string title, string message, NotificationType type, Guid? excludeUserId = null)
        {
            var favorites = await _favoriteRepository.GetListAsync(f => f.DestinationId == destinationId);

            foreach (var fav in favorites)
            {
                if (excludeUserId.HasValue && fav.UserId == excludeUserId.Value) continue;
                await NotifyUserWithPreferencesAsync(fav.UserId, title, message, message, type);
            }
        }

        private async Task NotifyUserWithPreferencesAsync(Guid userId, string title, string htmlMessage, string plainMessage, NotificationType notificationTypeEnum)
        {
            try
            {
                var user = await _userRepository.FindAsync(userId);
                if (user == null) return;

                bool notificationsEnabled = user.GetProperty<bool?>("ReceiveNotifications") ?? true;

                if (!notificationsEnabled)
                {
                    _logger.LogInformation($"🔕 Notificaciones desactivadas para el usuario {user.UserName}.");
                    return;
                }

                // Chequeamos CANAL (0=Pantalla, 1=Email, 2=Ambos)
                int channelPref = user.GetProperty<int?>("NotificationType") ?? 2;

                bool sendScreen = channelPref == 0 || channelPref == 2;
                bool sendEmail = channelPref == 1 || channelPref == 2;

                //PANTALLA
                if (sendScreen)
                {
                    await _notificationRepository.InsertAsync(new AppNotification(
                        GuidGenerator.Create(),
                        userId,
                        title,
                        plainMessage,
                        notificationTypeEnum.ToString()
                    ));
                }

                //EMAIL
                if (sendEmail && !string.IsNullOrWhiteSpace(user.Email))
                {
                    await _emailSender.SendAsync(
                        user.Email,
                        title,
                        $"<h3>{title}</h3><p>{htmlMessage}</p>"
                    );
                    _logger.LogInformation($"📧 Email enviado a {user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"🔥 Error enviando notificación al usuario {userId}: {ex.Message}");
            }
        }
    }
}