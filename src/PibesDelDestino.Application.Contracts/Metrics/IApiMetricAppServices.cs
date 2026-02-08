using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Metrics
{
    // Usamos IReadOnlyAppService porque desde el front solo vamos a LEER (no crear ni editar manualmente)
    public interface IApiMetricAppService : IReadOnlyAppService<ApiMetricDto, Guid, GetApiMetricsInput>
    {
        Task<DashboardDto> GetDashboardStatsAsync();
    }

}
