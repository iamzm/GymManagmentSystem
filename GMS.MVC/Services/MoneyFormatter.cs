using Microsoft.Extensions.Options;

namespace GMS.MVC.Services {
    /// <summary>
    /// Formats money for display. The currency lives in configuration rather than in two dozen
    /// views, because it has already changed country once — screens ask for a formatted amount
    /// and never spell out a currency themselves.
    /// </summary>
    public interface IMoneyFormatter {
        /// <summary>e.g. "3,500 PKR".</summary>
        string Format(decimal amount);

        /// <summary>The Amount On Its Own, For Places That Show The Code Separately.</summary>
        string Amount(decimal amount);

        /// <summary>The ISO Code, For Labels Like "Price (PKR)".</summary>
        string Code { get; }
    }

    public class MoneyFormatter(IOptions<CurrencyOptions> options) : IMoneyFormatter {
        private readonly CurrencyOptions _options = options.Value;

        public string Code => _options.Code;

        public string Amount(decimal amount) => amount.ToString(_options.Format);

        public string Format(decimal amount) => _options.CodeBeforeAmount
            ? $"{Code} {Amount(amount)}"
            : $"{Amount(amount)} {Code}";
    }

    /// <summary>Bound From The <c>Gym:Currency</c> Configuration Section.</summary>
    public class CurrencyOptions {
        public const string SectionName = "Gym:Currency";

        /// <summary>ISO Code Shown Beside Every Amount.</summary>
        public string Code { get; set; } = "PKR";

        /// <summary>Standard Numeric Format String; "N0" Means Thousands Separators, No Decimals.</summary>
        public string Format { get; set; } = "N0";

        /// <summary>True Renders "PKR 3,500"; False Renders "3,500 PKR".</summary>
        public bool CodeBeforeAmount { get; set; }
    }
}
