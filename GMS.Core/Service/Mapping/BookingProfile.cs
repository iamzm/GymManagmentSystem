using AutoMapper;
using Domin.GymEntities;
using Shared.DTOs.BookingDTOs;

namespace Services.Mapping {
    public class BookingProfile : Profile {
        public BookingProfile() {

            CreateMap<MemberSession, BookingDTO>()
                // CreatedAt Is Persisted As The BookingDate Column.
                .ForMember(dest => dest.BookedOn, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.Name))
                .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.Email))
                .ForMember(dest => dest.MemberPhoto, opt => opt.MapFrom(src => src.Member.Photo))
                .ForMember(dest => dest.SessionCategory, opt => opt.MapFrom(src => src.Session.SessionCategory.CategoryName))
                .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.Session.SessionTrainer.Name))
                .ForMember(dest => dest.SessionStart, opt => opt.MapFrom(src => src.Session.StartDate))
                .ForMember(dest => dest.SessionEnd, opt => opt.MapFrom(src => src.Session.EndDate));

            CreateMap<Session, ScheduleSlotDTO>()
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.SessionCategory.CategoryName))
                .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.SessionTrainer.Name))
                // Filled In By The Service From A Single Grouped Count Query.
                .ForMember(dest => dest.BookedSlots, opt => opt.Ignore());
        }
    }
}
