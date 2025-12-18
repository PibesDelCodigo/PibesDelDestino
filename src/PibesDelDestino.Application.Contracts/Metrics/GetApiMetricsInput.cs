using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Metrics
{
    public class GetApiMetricsInput : PagedAndSortedResultRequestDto
    {
        public string? ServiceName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}