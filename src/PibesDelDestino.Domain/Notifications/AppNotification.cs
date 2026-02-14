using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Notifications
{
    public class AppNotification : CreationAuditedEntity<Guid>
    {
        public Guid UserId { get; set; }    
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;

        public string Type { get; set; } 

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