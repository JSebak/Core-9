using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Business.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDetailsDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentUserId)).ReverseMap();
        }
    }

}
