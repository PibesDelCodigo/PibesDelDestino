using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Application.Contracts.Destinations
{
    public class CoordinatesDto
    {
        [Required(ErrorMessage = "The latitude is required.")]
        [Range(-90.0, 90.0, ErrorMessage = "The latitude must be between -90 and 90 degrees.")]
        public float Latitude { get; set; }

        [Required(ErrorMessage = "The longitude is required.")]
        [Range(-180.0, 180.0, ErrorMessage = "The longitude must be between -180 and 180 degrees.")]
        public float Longitude { get; set; }
    }
}