using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PibesDelDestino.Destinations;
using PibesDelDestino.Favorites;
using PibesDelDestino.TicketMaster;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing; // 👈 Importante para enviar mails
using Volo.Abp.Threading;
using Volo.Abp.Uow;      // 👈 Importante para la base de datos

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

            // ⏰ TIEMPO:
            // 86400000 = 24 Horas (Modo Producción)
            // 5000 = 5 Segundos (Modo Prueba - cambialo si querés probar ya)
            //Timer.Period = 86400000;
            Timer.Period = 5000;
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

                        // 4. Lógica de Negocio
                        var favoriteQuery = await favoriteRepo.GetQueryableAsync();
                        var activeDestinationIds = favoriteQuery
                            .Select(f => f.DestinationId)
                            .Distinct()
                            .ToList();

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

                                        var targetEmail = "mateofleglerutn@gmail.com"; // <--- PONÉ TU MAIL ACÁ SI NO LO LEES DE CONFIG

                                        var subject = $"¡Planazo en {destination.Name}!";
                                        var body = $"<h3>¡Hola viajero!</h3> " +
                                                   $"<p>Encontramos eventos imperdibles en <b>{destination.Name}</b>:</p>" +
                                                   $"<ul>";

                                        foreach (var evt in events)
                                        {
                                            body += $"<li>📅 <b>{evt.Name}</b> ({evt.Date}) - <a href='{evt.Url}'>Ver Entradas</a></li>";
                                        }
                                        body += "</ul>";

                                        // C. Enviar Email
                                        await emailSender.SendAsync(targetEmail, subject, body);

                                        Logger.LogInformation($"   -> 📧 Email enviado exitosamente a {targetEmail}");
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