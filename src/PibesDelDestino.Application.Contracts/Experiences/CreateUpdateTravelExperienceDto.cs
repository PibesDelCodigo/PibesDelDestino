using System;
using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Experiences
{
    public class CreateUpdateTravelExperienceDto
    {
        [Required]
        public Guid DestinationId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string Description { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Range(1, 5)]
        public int Rating { get; set; } 
    }
}