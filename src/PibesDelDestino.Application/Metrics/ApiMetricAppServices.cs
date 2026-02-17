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

        protected override async Task<IQueryable<ApiMetric>> CreateFilteredQueryAsync(GetApiMetricsInput input)
        {
            var query = await base.CreateFilteredQueryAsync(input);

            if (!string.IsNullOrEmpty(input.ServiceName))
            {
                query = query.Where(x => x.ServiceName.Contains(input.ServiceName));
            }

            // Ordenamiento por defecto: Lo más nuevo primero
            return query.OrderByDescending(x => x.CreationTime);
        }

        // Este método es el que se encargará de obtener las estadísticas para el dashboard.
        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            // No traemos la lista, solo pedimos el número total.
            var totalCalls = await Repository.CountAsync();

            // Usamos GetQueryable para armar la consulta SQL antes de ejecutarla.
            var query = await Repository.GetQueryableAsync();

            // Traemos solo los últimos 100 registros a memoria.
            var recentLogs = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.CreationTime).Take(100)
            );

            // Calculamos estadísticas sobre esa muestra pequeña de 100 ítems
            double successRate = 0;
            double avgTime = 0;

            if (recentLogs.Any())
            {
                successRate = (double)recentLogs.Count(x => x.IsSuccess) / recentLogs.Count * 100;
                avgTime = recentLogs.Average(x => x.ResponseTimeMs);
            }

            // Usamos LINQ to SQL para agrupar y contar en el servidor de BD.
            var historyQuery = await _searchHistoryRepo.GetQueryableAsync();

            var top5List = await AsyncExecuter.ToListAsync(
                historyQuery
                    .GroupBy(x => x.Term)
                    .Select(g => new { Term = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
            );

            // Convertimos el resultado anónimo a un Diccionario
            var topSearches = top5List.ToDictionary(k => k.Term, v => v.Count);

            return new DashboardDto
            {
                TotalApiCalls = (int)totalCalls,
                SuccessRate = successRate,
                AvgResponseTime = avgTime,
                TopSearches = topSearches
            };
        }
    }
}

