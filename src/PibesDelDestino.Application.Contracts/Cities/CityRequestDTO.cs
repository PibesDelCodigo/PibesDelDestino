using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.Cities
{
    public class CityRequestDTO
    {
        public string PartialName { get; set; }
        public int? MinPopulation { get; set; }
        public string? CountryId { get; set; } // Código de país (ej: 'AR', 'US')
    }
}
