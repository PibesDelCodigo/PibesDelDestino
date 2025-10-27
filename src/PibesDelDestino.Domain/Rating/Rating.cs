using System;
using PibesDelDestino.Users;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Ratings
{
    public class Rating : AuditedAggregateRoot<Guid>, IUserOwned
    {
        public Guid DestinationId { get; private set; }
        public int Score { get; private set; } 
        public string Comment { get; private set; }
        public Guid UserId { get; set; }

        private Rating() { /* Requerido por EF Core */ }

        public Rating(Guid id, Guid destinationId, Guid userId, int score, string comment)
            : base(id)
        {
            // Aplicamos tus mismas prácticas de validación:

            // 1. Validamos los GUIDs (similar a tu chequeo de "coordinates"
            //    que no sea nulo)
            DestinationId = destinationId == Guid.Empty
                ? throw new ArgumentException("DestinationId cannot be empty", nameof(destinationId))
                : destinationId;

            UserId = userId == Guid.Empty
                ? throw new ArgumentException("UserId cannot be empty", nameof(userId))
                : userId;

            // 2. Validamos el rango del Score (similar a tu "ValidateRange"
            //    de Coordinates)
            if (score < 1 || score > 5)
            {
                throw new ArgumentException("Score must be between 1 and 5.", nameof(score));
            }
            Score = score;

            // 3. Validamos el comentario (similar a tu "Name" de Destination,
            //    pero permitiendo que sea nulo o vacío, solo limitando la longitud)
            Comment = Check.Length(comment, nameof(comment), maxLength: 1000);
        }
    }
}