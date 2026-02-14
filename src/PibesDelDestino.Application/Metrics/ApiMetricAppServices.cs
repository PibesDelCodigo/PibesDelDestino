using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Metrics
{

    public class ApiMetricAppService : ReadOnlyAppService<ApiMetric, ApiMetricDto, Guid, GetApiMetricsInput>, IApiMetricAppService
    {
        // Inyectamos el repositorio de SearchHistory para poder acceder a los datos de historial de búsqueda y calcular las estadísticas de negocio.
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;

        public ApiMetricAppService(IRepository<ApiMetric, Guid> repository, IRepository<SearchHistory, Guid> searchHistoryRepo) : base(repository)
        {
            _searchHistoryRepo = searchHistoryRepo;
        }


        // Sobrescribimos el método para crear la consulta filtrada. Si el usuario ha proporcionado un nombre de servicio, filtramos los registros
        // para incluir solo aquellos cuyo nombre de servicio contenga el texto proporcionado.
        protected override async Task<IQueryable<ApiMetric>> CreateFilteredQueryAsync(GetApiMetricsInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (!string.IsNullOrEmpty(input.ServiceName))
            {
                // Si el usuario ha proporcionado un nombre de servicio, filtramos los registros para incluir
                // solo aquellos cuyo nombre de servicio contenga el texto proporcionado.
                query = query.Where(x => x.ServiceName.Contains(input.ServiceName));
            }

            query = query.OrderByDescending(x => x.CreationTime);

            return query;
        }

        // Este método es el que se encargará de obtener las estadísticas para el dashboard.
        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            // Datos Técnicos (API Metrics)
            var apiLogs = await Repository.GetListAsync();
            var recentLogs = apiLogs.OrderByDescending(x => x.CreationTime).Take(100).ToList();

            // Datos de Negocio (Search History)
            var searchLogs = await _searchHistoryRepo.GetListAsync();

            // Calculamos el Top 5
            var top5 = searchLogs
                .GroupBy(x => x.Term)
                .Select(g => new { Term = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionary(k => k.Term, v => v.Count);

            // Empaquetamos todo
            return new DashboardDto
            {
                TotalApiCalls = apiLogs.Count,
                SuccessRate = recentLogs.Any() ? (double)recentLogs.Count(x => x.IsSuccess) / recentLogs.Count * 100 : 0,
                AvgResponseTime = recentLogs.Any() ? recentLogs.Average(x => x.ResponseTimeMs) : 0,
                TopSearches = top5
            };
        }
    }
}
