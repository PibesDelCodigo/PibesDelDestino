using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.Cities
{
    public class CityDto
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string Region { get; set; } 
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public int Population { get; set; } 
    }
}

