using Domin.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.ExpenseDTOs {
    public class CreateExpenseDTO : IValidatableObject {

        [Required(ErrorMessage = "Title Is Required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Category Is Required")]
        [EnumDataType(typeof(ExpenseCategory))]
        public ExpenseCategory Category { get; set; }

        [Required(ErrorMessage = "Amount Is Required")]
        [Range(0.01, 100000000, ErrorMessage = "Amount Must Be Greater Than Zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Date Is Required")]
        [DataType(DataType.Date)]
        [Display(Name = "Spent On")]
        public DateOnly SpentOn { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "Payment Method Is Required")]
        [EnumDataType(typeof(PaymentMethod))]
        [Display(Name = "Paid By")]
        public PaymentMethod PaymentMethod { get; set; }

        [StringLength(80)]
        public string? Vendor { get; set; }

        [StringLength(50)]
        [Display(Name = "Invoice / Reference")]
        public string? Reference { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            // A Future-Dated Expense Would Quietly Distort The Current Month's Profit Figure.
            if (SpentOn > DateOnly.FromDateTime(DateTime.Now))
                yield return new ValidationResult(
                    "An Expense Cannot Be Dated In The Future.", [nameof(SpentOn)]);
        }
    }
}
