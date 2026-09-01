using AutoMapper;
using Domin.GymEntities;
using Shared.DTOs.MembershipDTOs;

namespace Services.Mapping {
    public class MembershipProfile : Profile {
        public MembershipProfile() {

            CreateMap<MemberShip, MembershipDTO>()
                // CreatedAt Is Persisted As The Contract's StartDate Column.
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.Name))
                .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.Email))
                .ForMember(dest => dest.MemberPhoto, opt => opt.MapFrom(src => src.Member.Photo))
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.Name));

            CreateMap<MemberShip, MembershipDetailsDTO>()
                .IncludeBase<MemberShip, MembershipDTO>()
                .ForMember(dest => dest.MemberPhone, opt => opt.MapFrom(src => src.Member.Phone))
                .ForMember(dest => dest.PlanDescription, opt => opt.MapFrom(src => src.Plan.Dsescription))
                .ForMember(dest => dest.PlanDurationDays, opt => opt.MapFrom(src => src.Plan.DurationDays))
                .ForMember(dest => dest.PlanCurrentPrice, opt => opt.MapFrom(src => src.Plan.Price));

            CreateMap<Member, MemberSelectDTO>();
            CreateMap<Plan, PlanSelectDTO>();
        }
    }
}
