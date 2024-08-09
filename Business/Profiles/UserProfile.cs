using AutoMapper;
using Domain.Entities;
using Domain.Models;

namespace Business.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDetailsDto>().ReverseMap();
        }
    }

}
