using Shared.DTOs.ExpenseDTOs;

namespace Services.Abstraction.Contract {
    public interface IExpenseService {
        Task<IEnumerable<ExpenseDTO>> GetAllExpenses(string? search = null, int? category = null, string? period = null);
        Task<ExpenseDTO?> GetExpenseDetails(int expenseId);
        Task<bool> CreateExpense(CreateExpenseDTO createdExpense);
        Task<ExpenseToUpdateDTO?> GetExpenseToUpdate(int expenseId);
        Task<bool> UpdateExpense(ExpenseToUpdateDTO updatedExpense, int expenseId);
        Task<bool> RemoveExpense(int expenseId);

        Task<ExpenseSummaryDTO> GetSummary(int? category = null, string? period = null);

        /// <summary>Revenue Against Expenses For The Last N Months, Oldest First.</summary>
        Task<IEnumerable<ProfitPointDTO>> GetProfitTrend(int months = 6);
    }
}
