using AutoMapper;
using Domin.Entities;
using Domin.GymEntities;
using Shared.DTOs.EmployeeDTOs;
using Shared.Extensions;

namespace Services.Mapping {
    public class EmployeeProfile : Profile {
        public EmployeeProfile() {

            CreateMap<CreateEmployeeDTO, Employee>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => new Address {
                    BuildingNumber = src.BuildingNumber,
                    Street = src.Street,
                    City = src.City
                }));

            CreateMap<Employee, EmployeeDTO>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.GetDisplayName()))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle.GetDisplayName()))
                .ForMember(dest => dest.EmploymentType, opt => opt.MapFrom(src => src.EmploymentType.GetDisplayName()))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth.ToString("MMM dd, yyyy")))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src =>
                    $"({src.Address.BuildingNumber}) {src.Address.Street}, {src.Address.City}"))
                // Hire Date Is Stored In CreatedAt, The Same Way Trainer Does It.
                .ForMember(dest => dest.HiredOn, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<Employee, EmployeeToUpdateDTO>()
                .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Address.Street))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address.City))
                .ForMember(dest => dest.BuildingNumber, opt => opt.MapFrom(src => src.Address.BuildingNumber));
        }
    }
}
