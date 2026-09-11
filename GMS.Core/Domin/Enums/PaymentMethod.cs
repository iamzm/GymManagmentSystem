using System.ComponentModel.DataAnnotations;

namespace Domin.Enums {
    public enum PaymentMethod {
        [Display(Name = "Cash")]
        Cash = 1,

        [Display(Name = "Bank Transfer")]
        BankTransfer = 2,

        [Display(Name = "Debit / Credit Card")]
        Card = 3,

        [Display(Name = "Cheque")]
        Cheque = 4,

        [Display(Name = "JazzCash")]
        JazzCash = 5,

        [Display(Name = "Easypaisa")]
        Easypaisa = 6,
    }
}
