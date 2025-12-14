using System;
using System.ComponentModel.DataAnnotations;

namespace PibesDelDestino.Favorites
{
    public class CreateFavoriteDto
    {
        [Required]
        public Guid DestinationId { get; set; }
    }
}