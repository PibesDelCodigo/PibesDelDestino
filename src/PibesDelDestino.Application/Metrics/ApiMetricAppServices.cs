using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PibesDelDestino.Metrics
{
    public class ApiMetricAppService : ReadOnlyAppService<ApiMetric, ApiMetricDto, Guid, GetApiMetricsInput>, IApiMetricAppService
    {
        private readonly IRepository<SearchHistory, Guid> _searchHistoryRepo;

        public ApiMetricAppService(IRepository<ApiMetric, Guid> repository, IRepository<SearchHistory, Guid> searchHistoryRepo) : base(repository)
        {
            _searchHistoryRepo = searchHistoryRepo;
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

        // 👇 LA MAGIA: Calculamos todo para el Dashboard
        public async Task<DashboardDto> GetDashboardStatsAsync()
        {
            // 1. Datos Técnicos (API Metrics)
            var apiLogs = await Repository.GetListAsync();
            var recentLogs = apiLogs.OrderByDescending(x => x.CreationTime).Take(100).ToList();

            // 2. Datos de Negocio (Search History)
            var searchLogs = await _searchHistoryRepo.GetListAsync();

            // Calculamos el Top 5
            var top5 = searchLogs
                .GroupBy(x => x.Term)
                .Select(g => new { Term = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToDictionary(k => k.Term, v => v.Count);

            // 3. Empaquetamos todo
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
