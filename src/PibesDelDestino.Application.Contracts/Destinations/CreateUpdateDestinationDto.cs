using PibesDelDestino.Application.Contracts.Destinations;
using System;
using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Application.Contracts.Destinations
{
    public class CreateUpdateDestinationDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Country must be between 1 and 100 characters.")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "City must be between 1 and 100 characters.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Population is required.")]
        [Range(0, uint.MaxValue, ErrorMessage = "Population must be a non-negative number.")]
        public uint Population { get; set; }

        [StringLength(500, ErrorMessage = "Photo URL must not exceed 500 characters.")]
        public string Photo { get; set; } = string.Empty;

        [Required(ErrorMessage = "UpdateDate is required.")]
        public DateTime UpdateDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Coordinates are required.")]
        public CoordinatesDto Coordinates { get; set; }
    }
}