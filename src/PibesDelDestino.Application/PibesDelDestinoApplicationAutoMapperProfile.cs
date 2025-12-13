using AutoMapper;
using PibesDelDestino.Application.Contracts.Destinations;
using PibesDelDestino.Destinations;
using PibesDelDestino.Ratings;
using PibesDelDestino.Users;
using Volo.Abp.Identity;
using PibesDelDestino.Experiences;  

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
        CreateMap<IdentityUser, PublicUserDto>();
        CreateMap<TravelExperience, TravelExperienceDto>()
    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => "Usuario Anónimo"));
        CreateMap<CreateUpdateTravelExperienceDto, TravelExperience>();

    }
}