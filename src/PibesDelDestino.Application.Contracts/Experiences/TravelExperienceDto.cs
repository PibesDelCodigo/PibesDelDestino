using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Experiences
{
    public class TravelExperienceDto : AuditedEntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } // Agregamos esto para saber quién escribió sin hacer mil consultas
        public Guid DestinationId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int Rating { get; set; }
    }
}