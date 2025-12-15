using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Notifications
{
    public class AppNotification : CreationAuditedEntity<Guid>
    {
        public Guid UserId { get; set; }        // ¿Para quién es?
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;

        // NUEVO: Para cumplir con 6.3 (Preferencias) y distinguir 6.1 de 6.2
        public string Type { get; set; } // Ej: "Comment", "DestinationUpdate", "System"

        private AppNotification() { }

        public AppNotification(Guid id, Guid userId, string title, string message, string type)
            : base(id)
        {
            UserId = userId;
            Title = title;
            Message = message;
            Type = type;
            IsRead = false;
        }
    }
}