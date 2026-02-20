using Shouldly;
using System;
using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;
using Volo.Abp.Domain.Repositories;
using System.Linq;

namespace PibesDelDestino.Metrics
{
    public abstract class ApiMetricAppService_Tests<TStartupModule> : PibesDelDestinoApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        protected readonly IApiMetricAppService _apiMetricAppService;
        protected readonly IRepository<ApiMetric, Guid> _apiMetricRepository;
        protected readonly IRepository<SearchHistory, Guid> _searchHistoryRepository;

        protected ApiMetricAppService_Tests()
        {
            _apiMetricAppService = GetRequiredService<IApiMetricAppService>();
            _apiMetricRepository = GetRequiredService<IRepository<ApiMetric, Guid>>();
            _searchHistoryRepository = GetRequiredService<IRepository<SearchHistory, Guid>>();
        }

        [Fact]
        public async Task Should_Filter_Metrics_By_ServiceName()
        {
            // Arrange
            // Agregamos string.Empty al final para que SQLite no explote
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "MyMemory", "/get", true, 100, string.Empty));
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "OtherApi", "/post", true, 200, string.Empty));

            // Act
            var result = await _apiMetricAppService.GetListAsync(new GetApiMetricsInput { ServiceName = "MyMemory" });

            // Assert
            result.TotalCount.ShouldBe(1);
            result.Items[0].ServiceName.ShouldBe("MyMemory");
        }

        [Fact]
        public async Task GetDashboardStatsAsync_Should_Calculate_Correct_Aggregations()
        {
            // Arrange - Insertamos métricas técnicas (4 éxitos, 1 fallo)
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "S1", "/e1", true, 100, string.Empty));
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "S1", "/e1", true, 200, string.Empty));
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "S1", "/e1", true, 300, string.Empty));
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "S1", "/e1", true, 400, string.Empty));
            await _apiMetricRepository.InsertAsync(new ApiMetric(Guid.NewGuid(), "S1", "/e1", false, 500, "Error"));

            // Arrange - Insertamos historial de búsqueda (3 para 'Tandil', 1 para 'Cataratas')
            await _searchHistoryRepository.InsertAsync(new SearchHistory(Guid.NewGuid(), "Tandil", 10));
            await _searchHistoryRepository.InsertAsync(new SearchHistory(Guid.NewGuid(), "Tandil", 5));
            await _searchHistoryRepository.InsertAsync(new SearchHistory(Guid.NewGuid(), "Tandil", 2));
            await _searchHistoryRepository.InsertAsync(new SearchHistory(Guid.NewGuid(), "Cataratas", 20));

            // Act
            var stats = await _apiMetricAppService.GetDashboardStatsAsync();

            // Assert Técnicos
            stats.TotalApiCalls.ShouldBe(5);
            stats.SuccessRate.ShouldBe(80); // 4 de 5 es 80%
            stats.AvgResponseTime.ShouldBe(300); // (100+200+300+400+500) / 5 = 300

            // Assert Negocio (Top Searches)
            stats.TopSearches.ContainsKey("Tandil").ShouldBeTrue();
            stats.TopSearches["Tandil"].ShouldBe(3); // Se buscó 3 veces
            stats.TopSearches["Cataratas"].ShouldBe(1);

            // Verificamos el orden: Tandil debería estar primero
            stats.TopSearches.First().Key.ShouldBe("Tandil");
        }
    }
}