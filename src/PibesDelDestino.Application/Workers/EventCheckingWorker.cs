using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            // 24 horas en milisegundos
            // Timer.Period = 86400000;
            Timer.Period = 30000;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation($"🕵️‍♂️ WORKER: Iniciando búsqueda de eventos a las {DateTime.Now}...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                // Usamos una UoW para que las consultas a repositorios funcionen correctamente
                using (var uow = unitOfWorkManager.Begin())
                {
                    try
                    {
                        var destinationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Destination, Guid>>();
                        var favoriteRepo = scope.ServiceProvider.GetRequiredService<IRepository<FavoriteDestination, Guid>>();
                        var ticketMasterService = scope.ServiceProvider.GetRequiredService<ITicketMasterService>();
                        var notificationManager = scope.ServiceProvider.GetRequiredService<NotificationManager>();

                        // 1. Obtener solo destinos que REALMENTE tienen seguidores
                        var activeDestinationIds = (await favoriteRepo.GetQueryableAsync())
                            .Select(f => f.DestinationId)
                            .Distinct()
                            .ToList();

                        if (!activeDestinationIds.Any())
                        {
                            Logger.LogInformation("WORKER: No hay favoritos activos para procesar.");
                            return;
                        }

                        var destinationsToCheck = await destinationRepo.GetListAsync(d => activeDestinationIds.Contains(d.Id));

                        foreach (var destination in destinationsToCheck)
                        {
                            try
                            {
                                // 2. Respetar la API externa: Pequeña pausa de 200ms para evitar bloqueos por Rate Limit
                                await Task.Delay(200);

                                var events = await ticketMasterService.SearchEventsAsync(destination.City);

                                if (events != null && events.Any())
                                {
                                    Logger.LogInformation($"WORKER: {events.Count} eventos nuevos en {destination.City}.");

                                    foreach (var evt in events)
                                    {
                                        // Aquí podrías añadir un check: if (!AlreadyNotified(evt.Id))
                                        await notificationManager.NotifyEventInCityAsync(
                                            destination.City,
                                            evt.Name,
                                            evt.Url
                                        );
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"⚠️ WORKER: Fallo puntual en ciudad {destination.City}: {ex.Message}");
                                // Continuamos con la siguiente ciudad, no matamos todo el proceso
                            }
                        }

                        await uow.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"🔥 WORKER FATAL ERROR: {ex.Message}");
                        // En caso de error crítico, no hacemos CompleteAsync para que ruede atrás (Rollback)
                    }
                }
            }
        }
    }
}