using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Ratings
{
    public class RatingDto : AuditedEntityDto<Guid>
    {
        public Guid DestinationId { get; set; }
        public Guid UserId { get; set; }
        public int Score { get; set; }
        public string Comment { get; set; }
        public string UserName { get; set; }
        public string UserProfilePicture { get; set; }
    }
}