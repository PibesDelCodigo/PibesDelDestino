using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Metrics
{
    public class ApiMetricDto : AuditedEntityDto<Guid>
    {
        public string ServiceName { get; set; }
        public string Endpoint { get; set; }
        public bool IsSuccess { get; set; }
        public int ResponseTimeMs { get; set; }
        public string ErrorMessage { get; set; }
    }
}