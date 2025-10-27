using System;
using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Ratings
{
    public class CreateRatingDto
    {
        [Required]
        public Guid DestinationId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
        public int Score { get; set; }

        [StringLength(1000)] // Opcional: limitar longitud del comentario
        public string Comment { get; set; }
    }
}