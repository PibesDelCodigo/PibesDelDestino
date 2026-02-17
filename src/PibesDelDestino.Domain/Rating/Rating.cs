using System;
using Volo.Abp; // Para BusinessException o ArgumentException
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Ratings
{
    public class Rating : AuditedEntity<Guid>
    {
        public Guid DestinationId { get; private set; }
        public Guid UserId { get; private set; }
        public int Score { get; private set; }
        public string Comment { get; private set; }

        private Rating() { }

        // Constructor público para crear
        public Rating(Guid id, Guid destinationId, Guid userId, int score, string comment)
            : base(id)
        {
            DestinationId = destinationId;
            UserId = userId;
            SetScore(score); 
            Comment = comment;
        }
        public void Update(int score, string comment)
        {
            SetScore(score);
            Comment = comment;
        }

        private void SetScore(int score)
        {
            if (score < 1 || score > 5)
            {
                // Protegemos la integridad del dominio
                throw new BusinessException("PibesDelDestino:Rating:InvalidScore", "El puntaje debe estar entre 1 y 5.");
            }
            Score = score;
        }
    }
}