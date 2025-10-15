using AutoMapper;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;

namespace PibesDelDestino;

public class BepixploreApplicationAutoMapperProfile : Profile
{
    public BepixploreApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Destination, DestinationDto>();
        CreateMap<CreateUpdateDestinationDto, Destination>();
        CreateMap<Coordinates, CoordinatesDto>();
        CreateMap<CoordinatesDto, Coordinates>();
    }
}