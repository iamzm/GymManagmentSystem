using AutoMapper;
using Domin.Enums;
using Domin.GymEntities;
using Shared.DTOs.InventoryDTOs;
using Shared.Extensions;

namespace Services.Mapping {
    public class InventoryProfile : Profile {
        public InventoryProfile() {

            CreateMap<CreateInventoryItemDTO, InventoryItem>();
            CreateMap<InventoryItemToUpdateDTO, InventoryItem>();

            CreateMap<InventoryItem, InventoryItemDTO>()
                .ForMember(dest => dest.Kind, opt => opt.MapFrom(src => src.Kind.GetDisplayName()))
                .ForMember(dest => dest.IsEquipment, opt => opt.MapFrom(src => src.Kind == ItemKind.Equipment))
                // Condition Is Null For Consumables, And GetDisplayName Cannot Be Called On It.
                .ForMember(dest => dest.Condition, opt => opt.MapFrom(src =>
                    src.Condition.HasValue ? src.Condition.Value.GetDisplayName() : null));

            CreateMap<InventoryItem, InventoryItemToUpdateDTO>();
        }
    }
}
