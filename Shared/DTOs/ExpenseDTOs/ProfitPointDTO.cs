namespace Shared.DTOs.ExpenseDTOs {
    /// <summary>One Month Of The Owner's Profit And Loss.</summary>
    public class ProfitPointDTO {
        public string Label { get; set; } = null!;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Net => Revenue - Expenses;

        /// <summary>
        /// True For The Month Still Running. Its Fixed Costs — Rent, Salaries — Are Already
        /// Booked While Only Part Of The Month's Sales Have Come In, So It Almost Always Shows
        /// A Loss. Flagged Rather Than Hidden, Because Leaving It Unmarked Makes A Normal
        /// Mid-Month Position Look Like The Gym Is Failing.
        /// </summary>
        public bool IsInProgress { get; set; }
        public bool IsProfit => Net >= 0;

        /// <summary>Net As A Share Of Revenue. Null With No Revenue, Because A Margin On Nothing
        /// Is Not 0% — It Is Undefined, And Showing 0% Would Read As Breaking Even.</summary>
        public int? MarginPercent
            => Revenue <= 0 ? null : (int)Math.Round(Net * 100m / Revenue);
    }
}
