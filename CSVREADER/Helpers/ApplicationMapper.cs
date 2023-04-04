using AutoMapper;
using CSVREADER.Models;

namespace CSVREADER.Helpers
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper()
        {

            CreateMap<CSV, Staff>()
                .ForMember(dest => dest.StFirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.StLastName, opt => opt.MapFrom(src => src.Staff_LastName))
                .ForMember(dest => dest.StContactNo, opt => opt.MapFrom(src => src.Staff_ContactNo))
                .ForMember(dest => dest.StEmailName, opt => opt.MapFrom(src => src.EmailAddress))
                .ForMember(dest => dest.EmailVerifactionStatus, opt => opt.MapFrom(src => src.emailverifactionStatus))
                .ForMember(dest => dest.MobileVerificationStatus, opt => opt.MapFrom(src => src.mobileverificationStatus))
                .ForMember(dest => dest.StBirthdate, opt => opt.MapFrom(src => src.Birthdate))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.ActivityStatus));
        }
    }
}
