using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PibesDelDestino.Destinations;
using PibesDelDestino.Favorites;
using PibesDelDestino.Notifications;
using PibesDelDestino.TicketMaster;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace PibesDelDestino.Workers
{
    public class EventCheckingWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EventCheckingWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            Timer.Period = 86400000; // 24h
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation("🕵️‍♂️ WORKER: Buscando eventos...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                using (var uow = unitOfWorkManager.Begin())
                {
                    try
                    {
                        // 1. Resolver Servicios (Ahora inyectamos el Manager)
                        var destinationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Destination, Guid>>();
                        var favoriteRepo = scope.ServiceProvider.GetRequiredService<IRepository<FavoriteDestination, Guid>>();
                        var ticketMasterService = scope.ServiceProvider.GetRequiredService<ITicketMasterService>();
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                        var userRepo = scope.ServiceProvider.GetRequiredService<IIdentityUserRepository>();

                        // ✨ AQUÍ ESTÁ LA MAGIA: El Manager se encarga de todo lo de notificaciones
                        var notificationManager = scope.ServiceProvider.GetRequiredService<NotificationManager>();

                        // 2. Lógica de búsqueda (Idéntica a la tuya)
                        var favoriteQuery = await favoriteRepo.GetQueryableAsync();
                        var activeDestinationIds = favoriteQuery.Select(f => f.DestinationId).Distinct().ToList();

                        if (!activeDestinationIds.Any()) return;

                        var destinationsToCheck = await destinationRepo.GetListAsync(d => activeDestinationIds.Contains(d.Id));

                        foreach (var destination in destinationsToCheck)
                        {
                            try
                            {
                                var events = await ticketMasterService.SearchEventsAsync(destination.City);

                                if (events.Any())
                                {
                                    // 3. Notificación "Masiva" (El Manager sabe a quién avisarle por Ciudad)
                                    // NOTA: Esto reemplaza todo tu foreach de usuarios para la notificación en pantalla.
                                    // Aún podés mantener el envío de mails manual si querés personalizarlo mucho,
                                    // pero el Manager ya inserta la AppNotification por vos.

                                    // Para no duplicar notificaciones (porque el Manager notifica a TODOS en la ciudad),
                                    // podés elegir:
                                    // A) Dejar que el Manager haga todo (Mails + Notis) -> Ideal a futuro.
                                    // B) Usar el Manager solo para crear la Noti en BD y dejar tu lógica de mail acá.

                                    // Opción B (Híbrida para no romper tus mails):
                                    // Llamamos al Manager para que genere las alertas en el sistema.
                                    foreach (var evt in events)
                                    {
                                        await notificationManager.NotifyEventInCityAsync(
                                            destination.City,
                                            evt.Name,
                                            evt.Url
                                        );
                                    }

                                    // Tu lógica de Mails existente (La dejamos intacta para asegurar el mail)
                                    var followers = await favoriteRepo.GetListAsync(f => f.DestinationId == destination.Id);
                                    foreach (var follower in followers)
                                    {
                                        var user = await userRepo.FindAsync(follower.UserId);
                                        // ... (Tu lógica de envío de mail sigue acá igual que antes) ...
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"❌ Error en {destination.Name}: {ex.Message}");
                            }
                        }

                        await uow.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"🔥 Error CRÍTICO: {ex.Message}");
                    }
                }
            }
        }
    }
}