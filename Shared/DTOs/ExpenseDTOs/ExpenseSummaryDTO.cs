namespace Shared.DTOs.ExpenseDTOs {
    /// <summary>The Figures Along The Top Of The Expenses Page.</summary>
    public class ExpenseSummaryDTO {
        public decimal TotalSpent { get; set; }
        public decimal SpentThisMonth { get; set; }
        public decimal SpentLastMonth { get; set; }
        public int EntryCount { get; set; }

        public List<CategoryTotalDTO> ByCategory { get; set; } = [];

        /// <summary>Positive Means This Month Is Costing More Than Last. Null When There Is No
        /// Previous Month To Compare Against, Which Is Not The Same As A 0% Change.</summary>
        public int? MonthOnMonthPercent
            => SpentLastMonth <= 0 ? null
             : (int)Math.Round((SpentThisMonth - SpentLastMonth) * 100m / SpentLastMonth);
    }

    public class CategoryTotalDTO {
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
        public int Percent { get; set; }
    }
}
