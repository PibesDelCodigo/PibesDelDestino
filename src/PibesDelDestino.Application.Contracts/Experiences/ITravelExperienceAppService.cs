using System;
using Volo.Abp.Application.Services;

namespace PibesDelDestino.Experiences
{
    public interface ITravelExperienceAppService :
        ICrudAppService< // Interfaz estándar de CRUD
            TravelExperienceDto,
            Guid,
            GetTravelExperiencesInput, // Usamos nuestro filtro nuevo
            CreateUpdateTravelExperienceDto>
    {
        
    }
}