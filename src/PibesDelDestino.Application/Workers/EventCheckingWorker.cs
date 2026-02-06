using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PibesDelDestino.Destinations;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using PibesDelDestino.TicketMaster;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing; // 👈 Importante para enviar mails
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Threading;
using Volo.Abp.Uow;     // 👈 Importante para la base de datos

namespace PibesDelDestino.Workers
{
    public class EventCheckingWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ILogger<EventCheckingWorker> Logger { get; set; }

        public EventCheckingWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;

            // 86400000 = 24 Horas 
            // 5000 = 5 Segundos (Prueba)
            Timer.Period = 15000;
            Logger = NullLogger<EventCheckingWorker>.Instance;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation("🕵️‍♂️ WORKER INICIADO: Buscando eventos y preparando mails...");

            // 1. Crear Scope Nuevo (Vida útil aislada)
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                // 2. Iniciar Unidad de Trabajo (Conexión DB persistente)
                using (var uow = unitOfWorkManager.Begin())
                {
                    try
                    {
                        // 3. Resolver Servicios
                        var destinationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Destination, Guid>>();
                        var favoriteRepo = scope.ServiceProvider.GetRequiredService<IRepository<FavoriteDestination, Guid>>();
                        var ticketMasterService = scope.ServiceProvider.GetRequiredService<ITicketMasterService>();
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>(); // 📧 Servicio de Mail
                        var userRepo = scope.ServiceProvider.GetRequiredService<IIdentityUserRepository>(); // 👤 Repo de Usuarios
                        var guidGenerator = scope.ServiceProvider.GetRequiredService<IGuidGenerator>();
                        var notificationRepo = scope.ServiceProvider.GetRequiredService<IRepository<AppNotification, Guid>>();

                        // 4. Lógica de Negocio
                        var favoriteQuery = await favoriteRepo.GetQueryableAsync();
                        var activeDestinationIds = favoriteQuery.Select(f => f.DestinationId).Distinct().ToList();

                        if (!activeDestinationIds.Any())
                        {
                            Logger.LogInformation("😴 Nadie tiene favoritos cargados.");
                            return;
                        }

                        var destinationsToCheck = await destinationRepo.GetListAsync(d => activeDestinationIds.Contains(d.Id));
                        Logger.LogInformation($"🎯 Analizando {destinationsToCheck.Count} ciudades favoritas.");

                        foreach (var destination in destinationsToCheck)
                        {
                            try
                            {
                                // A. Buscar Eventos
                                var events = await ticketMasterService.SearchEventsAsync(destination.City);

                                if (events.Any())
                                {
                                    // B. Buscar interesados
                                    var followers = await favoriteRepo.GetListAsync(f => f.DestinationId == destination.Id);
                                    Logger.LogInformation($"🎉 {events.Count} eventos en {destination.Name}. Enviando notificaciones...");

                                    foreach (var follower in followers)
                                    {
                                        // ⚠️ NOTA: Como aún no tenemos el mail del usuario en la tabla Favoritos,
                                        // usamos tu mail hardcodeado para la prueba.
                                        // En el futuro, haríamos: var email = await userRepo.GetEmailAsync(follower.UserId);

                                        var user = await userRepo.FindAsync(follower.UserId);


                                        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                                        {
                                            var recibeNotificaciones = user.ExtraProperties.ContainsKey("ReceiveNotifications") ? Convert.ToBoolean(user.ExtraProperties["ReceiveNotifications"]) : true;

                                            if (!recibeNotificaciones)
                                            {
                                                Logger.LogInformation($"🔕 Usuario {user.UserName} tiene notificaciones desactivadas. Omitiendo.");
                                                continue; 
                                            }

                                            var subject = $"¡Planazo en {destination.Name}!";
                                            var body = $"<h3>¡Hola viajero!</h3> " +
                                                       $"<p>Encontramos eventos imperdibles en <b>{destination.Name}</b>:</p>" +
                                                       $"<ul>";

                                            foreach (var evt in events)
                                            {
                                                body += $"<li>📅 <b>{evt.Name}</b> ({evt.Date}) - <a href='{evt.Url}'>Ver Entradas</a></li>";
                                            }
                                            body += "</ul>";

                                            var preferencia = user.GetProperty<string>("NotifPref") ?? "Ambas";

                                            bool enviarMail = preferencia == "Mail" || preferencia == "Ambas";
                                            bool enviarPantalla = preferencia == "Pantalla" || preferencia == "Ambas";

                                            if (enviarMail && !string.IsNullOrWhiteSpace(user.Email))
                                            {
                                                //Enviar Email
                                                await emailSender.SendAsync(user.Email, subject, body);
                                                Logger.LogInformation($"   -> 📧 Email enviado exitosamente a {user.Email}");
                                            }

                                            if (enviarPantalla)
                                            {
                                                //Crear Notificación en Pantalla
                                                var nuevaNotificacion = new AppNotification(
                                                    guidGenerator.Create(),      
                                                    user.Id,                     
                                                    "¡Nuevos Eventos!",          
                                                    $"Se encontraron {events.Count} eventos en {destination.City}.",
                                                    "EventAlert");

                                                nuevaNotificacion.IsRead = false;

                                                await notificationRepo.InsertAsync(nuevaNotificacion, autoSave: true);
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogWarning($"   -> ⚠️ No se pudo enviar email a UserId {follower.UserId} (Email no encontrado)");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"❌ Error procesando {destination.Name}: {ex.Message}");
                            }
                        }

                        // 5. Confirmar transacción
                        await uow.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"🔥 Error CRÍTICO en el Worker: {ex.Message}");
                    }
                }
            }

            Logger.LogInformation("😴 WORKER FINALIZADO.");
        }
    }
}