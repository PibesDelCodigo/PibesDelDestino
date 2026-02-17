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

        private Rating() { } // EF Core

        public Rating(Guid id, Guid destinationId, Guid userId, int score, string comment)
            : base(id)
        {

            DestinationId = destinationId == Guid.Empty
                ? throw new ArgumentException("DestinationId cannot be empty", nameof(destinationId))
                : destinationId;

            UserId = userId == Guid.Empty
                ? throw new ArgumentException("UserId cannot be empty", nameof(userId))
                : userId;

            if (score < 1 || score > 5)
            {
                throw new ArgumentException("Score must be between 1 and 5.", nameof(score));
            }
            Score = score;

            Comment = Check.Length(comment, nameof(comment), maxLength: 1000);
        }
    }
}