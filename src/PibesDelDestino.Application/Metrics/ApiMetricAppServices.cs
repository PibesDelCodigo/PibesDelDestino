using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Metrics
{
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