using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp; // Necesario para las validaciones (Check)

namespace PibesDelDestino.Experiences
{
    public class TravelExperience : FullAuditedAggregateRoot<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid DestinationId { get; private set; }

        // Mantenemos la seguridad: Solo se pueden cambiar desde adentro
        public string Title { get; private set; }
        public string Description { get; private set; }
        public DateTime Date { get; private set; }
        public int Rating { get; private set; }

        // Constructor...
        public TravelExperience(Guid id, Guid userId, Guid destinationId, string title, string description, DateTime date, int rating)
            : base(id)
        {
            UserId = userId;
            DestinationId = destinationId;
            Title = Check.NotNullOrWhiteSpace(title, nameof(title));
            Description = description;
            Date = date;
            Rating = rating;
        }

        // 👇 MÉTODO NUEVO: La forma correcta de editar 👇
        public void Update(string title, string description, int rating, DateTime date)
        {
            // Acá podrías agregar validaciones extra si quisieras
            // Ejemplo: if (rating < 1 || rating > 5) throw ...

            Title = Check.NotNullOrWhiteSpace(title, nameof(title));
            Description = description;
            Rating = rating;
            Date = date;
        }
    }
}