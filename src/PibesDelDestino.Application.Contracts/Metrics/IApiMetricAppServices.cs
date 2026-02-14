using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Metrics
{
    // Usamos IReadOnlyAppService porque desde el front solo vamos a leer
    public interface IApiMetricAppService : IReadOnlyAppService<ApiMetricDto, Guid, GetApiMetricsInput>
    {
        Task<DashboardDto> GetDashboardStatsAsync();
    }

}
