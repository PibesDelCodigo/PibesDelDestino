using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Favorites
{
    // Usamos CreationAudited para saber CUÁNDO le dio like, pero no necesitamos "Update" ni "Delete" soft
    public class FavoriteDestination : CreationAuditedAggregateRoot<Guid>
    {
        public Guid UserId { get; private set; }
        public Guid DestinationId { get; private set; }

        private FavoriteDestination() { }

        public FavoriteDestination(Guid id, Guid userId, Guid destinationId)
            : base(id)
        {
            UserId = userId;
            DestinationId = destinationId;
        }
    }
}