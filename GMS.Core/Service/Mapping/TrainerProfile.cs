using AutoMapper;
using Domin.Entities;
using Domin.GymEntities;
using Shared.DTOs.TrainerDTOs;
using Shared.Extensions;

namespace Services.Mapping {
    public class TrainerProfile : Profile {
        public TrainerProfile() {

            CreateMap<CreateTrainerDTO, Trainer>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Address {
                    BuildingNumber = src.BuildingNumber,
                    Street = src.Street,
                    City = src.City
                }));

            CreateMap<Trainer, TrainerDTO>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.GetDisplayName()))
                .ForMember(dest => dest.Specialties, opt => opt.MapFrom(src => src.Specialties.GetDisplayName()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToString("MMM dd, yyyy")))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
                    $"({src.Address.BuildingNumber}) {src.Address.Street}, {src.Address.City}"))
                // HireDate Is Stored In CreatedAt.
                .ForMember(dest => dest.HiredOn, opt => opt.MapFrom(src => src.CreatedAt))
                // Session Counts Are Filled In By The Service.
                .ForMember(dest => dest.SessionCount, opt => opt.Ignore())
                .ForMember(dest => dest.UpcomingSessionCount, opt => opt.Ignore());

            CreateMap<Trainer, TrainerToUpdateDTO>()
                .ForMember(dist => dist.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dist => dist.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dist => dist.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber));

            CreateMap<Trainer, TrainerSelectDTO>();
        }
    }
}
