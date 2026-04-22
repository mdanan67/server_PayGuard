using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using server.Dto;
using server.model;

namespace server.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<ParentRegistrationDto, User>()
                    .ForMember(des => des.Email, opt => opt.MapFrom(src => src.Email))
                    .ForMember(des => des.FirstName, opt => opt.MapFrom(src => src.FirstName))
                    .ForMember(des => des.LastName, opt => opt.MapFrom(src => src.LastName))
                    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                    .ForMember(dest => dest.Role, opt => opt.Ignore())
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());


        }
    }
}