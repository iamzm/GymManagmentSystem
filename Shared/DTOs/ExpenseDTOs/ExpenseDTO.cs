namespace Shared.DTOs.ExpenseDTOs {
    public class ExpenseDTO {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateOnly SpentOn { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string? Vendor { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }
}
