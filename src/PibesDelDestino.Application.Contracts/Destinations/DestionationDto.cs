using PibesDelDestino.Application.Contracts.Destinations;
using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Application.Contracts.Destinations
{
    public class DestinationDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public uint Population { get; set; }
        public string Photo { get; set; }
        public DateTime UpdateDate { get; set; }
        public CoordinatesDto Coordinates { get; set; }

        public double AverageRating { get; set; }
    }
}