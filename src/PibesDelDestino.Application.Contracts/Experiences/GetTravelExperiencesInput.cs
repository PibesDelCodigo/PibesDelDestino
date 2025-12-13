using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Experiences
{
    public class GetTravelExperiencesInput : PagedAndSortedResultRequestDto
    {
        public Guid? DestinationId { get; set; }

        // REQUERIMIENTO 3.6: Buscar por palabras clave
        public string FilterText { get; set; }

        // REQUERIMIENTO 3.5: Filtrar por tipo (Positiva/Negativa/Neutral)
        public ExperienceFilterType? Type { get; set; }
    }

    public enum ExperienceFilterType
    {
        Positive, // 4-5 estrellas
        Neutral,  // 3 estrellas
        Negative  // 1-2 estrellas
    }
}