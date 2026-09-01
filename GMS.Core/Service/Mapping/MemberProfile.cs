using AutoMapper;
using Domin.Entities;
using Domin.GymEntities;
using Shared.DTOs.MemberDTOs;
using Shared.Extensions;

namespace Services.Mapping {
    public class MemberProfile : Profile {
        public MemberProfile() {

            CreateMap<Member, MemberDTO>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.GetDisplayName()))
                // CreatedAt Is Persisted As The Member's JoinDate Column.
                .ForMember(dest => dest.JoinedOn, opt => opt.MapFrom(src => src.CreatedAt))
                // Subscription Fields Are Filled In By The Service From A Single Memberships Lookup.
                .ForMember(dest => dest.PlanName, opt => opt.Ignore())
                .ForMember(dest => dest.MembershipEndDate, opt => opt.Ignore());

            CreateMap<CreateMemberDTO, Member>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Address {
                    BuildingNumber = src.BuildingNumber,
                    City = src.City,
                    Street = src.Street
                }))
                .ForMember(dest => dest.HealthRecord, opt => opt.MapFrom(src => new HealthRecord {
                    Height = src.HealthRecordDTO.Height,
                    Weight = src.HealthRecordDTO.Weight,
                    BloodType = src.HealthRecordDTO.BloodType,
                    Note = src.HealthRecordDTO.Note
                }));

            CreateMap<Member, MemberDetailsDTO>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.GetDisplayName()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToString("MMM dd, yyyy")))
                .ForMember(dest => dest.JoinedOn, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
                    $"({src.Address.BuildingNumber}) {src.Address.Street}, {src.Address.City}"))
                // Membership And Activity Figures Are Resolved By The Service.
                .ForMember(dest => dest.PlanName, opt => opt.Ignore())
                .ForMember(dest => dest.MemberShipStartDate, opt => opt.Ignore())
                .ForMember(dest => dest.MemberShipEndDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalBookings, opt => opt.Ignore())
                .ForMember(dest => dest.TotalMemberships, opt => opt.Ignore());

            CreateMap<HealthRecord, HealthRecordDTO>();

            CreateMap<Member, MemberToUpdateDTO>()
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber))
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.HealthRecordDTO, opt => opt.MapFrom(src => src.HealthRecord));
        }
    }
}
