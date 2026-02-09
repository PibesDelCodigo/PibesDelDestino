// REQUERIMIENTO 7.1: Consultar métricas de uso.
// Endpoint exclusivo para Administradores.
// Recupera las estadísticas de consumo de la API externa (GeoDB)
// para monitorear costos y límites de uso.
// REQUERIMIENTO 7.1 (Registro): Monitoreo de API.
// Cada vez que consumimos el servicio externo, registramos el evento
// para actualizar las métricas de uso del sistema.
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Metrics
{
    //REQ 7.1 - Servicio de aplicación para consultar métricas
    public class ApiMetricAppService : ReadOnlyAppService<ApiMetric, ApiMetricDto, Guid, GetApiMetricsInput>, IApiMetricAppService
    {
        public ApiMetricAppService(IRepository<ApiMetric, Guid> repository) : base(repository)
        {
        }

        // Filtramos por nombre de servicio (Ej: ver solo traducciones)
        protected override async Task<IQueryable<ApiMetric>> CreateFilteredQueryAsync(GetApiMetricsInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (!string.IsNullOrEmpty(input.ServiceName))
            {
                query = query.Where(x => x.ServiceName.Contains(input.ServiceName));
            }

            // Ordenamos por fecha descendente (lo más nuevo primero)
            query = query.OrderByDescending(x => x.CreationTime);

            return query;
        }
    }
}