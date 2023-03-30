using AutoMapper;
using CSVREADER.Models;

namespace CSVREADER.Helpers
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper() {
            
            CreateMap<CSV, Staff>()
                .ForMember(dest => dest.Staff_Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Staff_FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.Staff_LastName, opt => opt.MapFrom(src => src.Staff_LastName))
                .ForMember(dest => dest.Staff_ContactNo, opt => opt.MapFrom(src => src.Staff_ContactNo));
        }
    }
}
