using AutoMapper;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;
using PibesDelDestino.Ratings;

namespace PibesDelDestino;

public class PibesDelDestinoApplicationAutoMapperProfile : Profile
{
    public PibesDelDestinoApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        CreateMap<Destination, DestinationDto>();
        CreateMap<CreateUpdateDestinationDto, Destination>();
        CreateMap<Coordinates, CoordinatesDto>();
        CreateMap<CoordinatesDto, Coordinates>();
        CreateMap<Rating, RatingDto>();
    }
}