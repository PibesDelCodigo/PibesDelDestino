using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace PibesDelDestino.Destinations
{
    public class Destination : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; }
        public string Country { get; private set; }
        public string City { get; private set; }
        public uint Population { get; private set; }
        public string Photo { get; private set; }
        public DateTime UpdateDate { get; private set; }
        public Coordinates Coordinates { get; private set; } // Coordenadas VO

        private Destination() { } // EF Core

        public Destination(Guid id,
            string name,
            string country,
            string city,
            uint population,
            string photo,
            DateTime updateDate,
            Coordinates coordinates)
            : base(id)
        {
            Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
            Country = Check.NotNullOrWhiteSpace(country, nameof(country), maxLength: 100);
            City = Check.NotNullOrWhiteSpace(city, nameof(city), maxLength: 100);
            Population = population;
            Photo = photo;
            UpdateDate = updateDate;
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        }
    }
}