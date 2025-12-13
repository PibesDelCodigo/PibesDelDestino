using System;
using PibesDelDestino.Users;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Experiences
{
    public class TravelExperience : FullAuditedAggregateRoot<Guid>, IUserOwned
    {
        public Guid UserId { get; set; }
        public Guid DestinationId { get; private set; }

        public string Title { get; private set; }
        public string Description { get; private set; }
        public DateTime Date { get; private set; }

        // CAMBIO: Ahora es un número entero del 1 al 5
        public int Rating { get; private set; }

        private TravelExperience() { }

        public TravelExperience(
            Guid id,
            Guid userId,
            Guid destinationId,
            string title,
            string description,
            DateTime date,
            int rating) // Recibimos el número
            : base(id)
        {
            UserId = userId;
            DestinationId = destinationId;
            Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 100);
            Description = Check.NotNullOrWhiteSpace(description, nameof(description), maxLength: 4000);
            Date = date;

            // VALIDACIÓN: Si mandan algo fuera de rango, explota (protección del backend)
            if (rating < 1 || rating > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(rating), "La calificación debe ser entre 1 y 5.");
            }

            Rating = rating;
        }
    }
}