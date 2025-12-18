using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Metrics
{
    // Usamos AuditedAggregateRoot para que guarde automáticamente quién lo creó y cuándo (CreationTime)
    public class ApiMetric : AuditedAggregateRoot<Guid>
    {
        public string ServiceName { get; set; } // Ej: "GoogleTranslate", "OpenWeather"
        public string Endpoint { get; set; }    // Ej: "/translate"
        public bool IsSuccess { get; set; }     // ¿Falló o funcionó?
        public int ResponseTimeMs { get; set; } // Cuánto tardó en milisegundos (opcional, pero queda pro)
        public string ErrorMessage { get; set; } // Si falló, guardamos por qué

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