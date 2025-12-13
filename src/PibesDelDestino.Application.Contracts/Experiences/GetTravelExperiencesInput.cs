using System;
using Volo.Abp.Application.Dtos;

namespace PibesDelDestino.Experiences
{
    public class GetTravelExperiencesInput : PagedAndSortedResultRequestDto
    {
        public Guid? DestinationId { get; set; }
        public string? FilterText { get; set; }
        public ExperienceFilterType? Type { get; set; }
    }

    public enum ExperienceFilterType
    {
        Positive, // 4-5 estrellas
        Neutral,  // 3 estrellas
        Negative  // 1-2 estrellas
    }
}