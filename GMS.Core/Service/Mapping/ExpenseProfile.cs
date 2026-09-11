using AutoMapper;
using Domin.GymEntities;
using Shared.DTOs.ExpenseDTOs;
using Shared.Extensions;

namespace Services.Mapping {
    public class ExpenseProfile : Profile {
        public ExpenseProfile() {

            CreateMap<CreateExpenseDTO, Expense>();
            CreateMap<ExpenseToUpdateDTO, Expense>();

            CreateMap<Expense, ExpenseDTO>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.GetDisplayName()))
                // The Raw Value Rides Along So The Filter Chips Can Mark Themselves Active.
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => (int)src.Category))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.GetDisplayName()));

            CreateMap<Expense, ExpenseToUpdateDTO>();
        }
    }
}
