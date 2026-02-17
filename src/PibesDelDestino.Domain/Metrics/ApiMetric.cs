using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Metrics
{
    public class ApiMetric : AuditedAggregateRoot<Guid>
    {
        public string ServiceName { get; set; } 
        public string Endpoint { get; set; }    
        public bool IsSuccess { get; set; }   
        public int ResponseTimeMs { get; set; } 
        public string ErrorMessage { get; set; } 

        protected ApiMetric() { }

        public ApiMetric(Guid id, string serviceName, string endpoint, bool isSuccess, int responseTimeMs, string errorMessage = null)
            : base(id)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
            IsSuccess = isSuccess;
            ResponseTimeMs = responseTimeMs;
            ErrorMessage = errorMessage;
        }
    }
}