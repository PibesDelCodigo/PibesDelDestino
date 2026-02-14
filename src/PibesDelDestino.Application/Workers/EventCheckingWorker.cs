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
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace PibesDelDestino.Workers
{
    public class EventCheckingWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        // Inyectamos el IServiceScopeFactory para poder resolver servicios dentro del DoWorkAsync.
        public EventCheckingWorker(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory)
            : base(timer, serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            Timer.Period = 86400000;
        }

        // Sobrescribimos el método DoWorkAsync para implementar la lógica de búsqueda de eventos y notificación a los usuarios.
        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Logger.LogInformation($"🕵️‍♂️ WORKER: Iniciando busqueda de eventos a las {DateTime.Now}...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                // Iniciamos una Unidad de Trabajo para asegurar que todas las operaciones de base de datos sean atómicas.
                //Esto significa que si algo falla en el proceso, no se guardará nada en la base de datos, evitando datos inconsistentes.
                var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

                using (var uow = unitOfWorkManager.Begin())
                {
                    try
                    {
                        // 1. Resolución de servicios (Repositorios, Servicios, etc.)
                        var destinationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Destination, Guid>>();
                        var favoriteRepo = scope.ServiceProvider.GetRequiredService<IRepository<FavoriteDestination, Guid>>();
                        var ticketMasterService = scope.ServiceProvider.GetRequiredService<ITicketMasterService>();
                        var notificationManager = scope.ServiceProvider.GetRequiredService<NotificationManager>();

                        // Lógica de búsqueda
                        var favoriteQuery = await favoriteRepo.GetQueryableAsync();
                        var activeDestinationIds = favoriteQuery.Select(f => f.DestinationId).Distinct().ToList();

                        if (!activeDestinationIds.Any())
                        {
                            Logger.LogDebug("WORKER: Nadie tiene favoritos activos.");
                            return;
                        }
                            
                        // Búsqueda de eventos para cada destino activo (Solo aquellos que tienen seguidores)
                        var destinationsToCheck = await destinationRepo.GetListAsync(d => activeDestinationIds.Contains(d.Id));

                        foreach (var destination in destinationsToCheck)
                        {
                            try
                            {
                                var events = await ticketMasterService.SearchEventsAsync(destination.City);

                                if (events.Any())
                                {
                                    Logger.LogInformation($"WORKER: Encontrados {events.Count} eventos en {destination.City}. Delegando al Manager");

                                    foreach (var evt in events)
                                    {
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
                                Logger.LogError($"❌ WORKER: Error buscando en {destination.City}: {ex.Message}");
                            }
                        }
                        // Si todo salió bien, completamos la Unidad de Trabajo para guardar los cambios en la base de datos.
                        await uow.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"🔥 WORKER CRITICAL: {ex.Message}");
                    }
                }
            }
        }
    }
}