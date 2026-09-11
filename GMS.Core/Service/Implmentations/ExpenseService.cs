using AutoMapper;
using Domin.Contract;
using Domin.Enums;
using Domin.GymEntities;
using Services.Abstraction.Contract;
using Shared.DTOs.ExpenseDTOs;
using Shared.Extensions;

namespace Services.Implmentations {
	public class ExpenseService(IUnitOfWork unitOfWork, IMapper mapper) : IExpenseService {

		public async Task<IEnumerable<ExpenseDTO>> GetAllExpenses(string? search = null, int? category = null, string? period = null) {
			var result = await Filtered(search, category, period);
			// Newest Spend First: An Expense List Is Read From The Top Down.
			return [.. result.OrderByDescending(E => E.SpentOn).ThenByDescending(E => E.Id)];
		}

		public async Task<ExpenseDTO?> GetExpenseDetails(int expenseId) {
			var expense = await unitOfWork.GetRepository<Expense>().GetAsync(expenseId);
			return expense is null ? null : mapper.Map<ExpenseDTO>(expense);
		}

		public async Task<bool> CreateExpense(CreateExpenseDTO createdExpense) {
			try {
				var expense = mapper.Map<Expense>(createdExpense);
				expense.CreatedAt = DateOnly.FromDateTime(DateTime.Now);
				expense.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				await unitOfWork.GetRepository<Expense>().AddAsync(expense);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<ExpenseToUpdateDTO?> GetExpenseToUpdate(int expenseId) {
			var expense = await unitOfWork.GetRepository<Expense>().GetAsync(expenseId);
			return expense is null ? null : mapper.Map<ExpenseToUpdateDTO>(expense);
		}

		public async Task<bool> UpdateExpense(ExpenseToUpdateDTO updatedExpense, int expenseId) {
			try {
				var repo = unitOfWork.GetRepository<Expense>();
				var expenseToUpdate = await repo.GetAsync(expenseId);
				if (expenseToUpdate is null) return false;

				expenseToUpdate.Title = updatedExpense.Title;
				expenseToUpdate.Category = updatedExpense.Category;
				expenseToUpdate.Amount = updatedExpense.Amount;
				expenseToUpdate.SpentOn = updatedExpense.SpentOn;
				expenseToUpdate.PaymentMethod = updatedExpense.PaymentMethod;
				expenseToUpdate.Vendor = updatedExpense.Vendor;
				expenseToUpdate.Reference = updatedExpense.Reference;
				expenseToUpdate.Notes = updatedExpense.Notes;
				expenseToUpdate.UpdatedAt = DateOnly.FromDateTime(DateTime.Now);

				repo.Update(expenseToUpdate);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<bool> RemoveExpense(int expenseId) {
			try {
				var repo = unitOfWork.GetRepository<Expense>();
				var expenseToRemove = await repo.GetAsync(expenseId);
				if (expenseToRemove is null) return false;

				repo.Delete(expenseToRemove);
				return await unitOfWork.SaveChangesAsync() > 0;
			} catch (Exception) {
				return false;
			}
		}

		public async Task<ExpenseSummaryDTO> GetSummary(int? category = null, string? period = null) {
			// The Headline Figures Follow Whatever The Page Is Filtered To, So They Describe What
			// Is On Screen Rather Than Contradicting It.
			var scoped = (await Filtered(null, category, period)).ToList();

			var today = DateOnly.FromDateTime(DateTime.Now);
			var monthStart = new DateOnly(today.Year, today.Month, 1);
			var lastMonthStart = monthStart.AddMonths(-1);

			var total = scoped.Sum(E => E.Amount);

			var byCategory = scoped
				.GroupBy(E => E.Category)
				.Select(G => new CategoryTotalDTO {
					Category = G.Key,
					Amount = G.Sum(E => E.Amount),
					Percent = total <= 0 ? 0 : (int)Math.Round(G.Sum(E => E.Amount) * 100m / total),
				})
				.OrderByDescending(C => C.Amount)
				.ToList();

			return new ExpenseSummaryDTO {
				TotalSpent = total,
				SpentThisMonth = scoped.Where(E => E.SpentOn >= monthStart).Sum(E => E.Amount),
				SpentLastMonth = scoped.Where(E => E.SpentOn >= lastMonthStart && E.SpentOn < monthStart).Sum(E => E.Amount),
				EntryCount = scoped.Count,
				ByCategory = byCategory,
			};
		}

		public async Task<IEnumerable<ProfitPointDTO>> GetProfitTrend(int months = 6) {
			var expenses = await unitOfWork.GetRepository<Expense>().GetAllAsync();
			var memberships = await unitOfWork.GetRepository<MemberShip>().GetAllAsync();

			var today = DateOnly.FromDateTime(DateTime.Now);
			var points = new List<ProfitPointDTO>();

			for (var back = months - 1; back >= 0; back--) {
				var cursor = new DateOnly(today.Year, today.Month, 1).AddMonths(-back);
				var next = cursor.AddMonths(1);

				// Revenue Is Recognised When The Contract Was Sold, Which Is MemberShip.CreatedAt —
				// The Same Basis The Existing Dashboard Revenue Figures Already Use.
				var revenue = memberships
					.Where(M => M.CreatedAt >= cursor && M.CreatedAt < next)
					.Sum(M => M.PricePaid);

				var spend = expenses
					.Where(E => E.SpentOn >= cursor && E.SpentOn < next)
					.Sum(E => E.Amount);

				points.Add(new ProfitPointDTO {
					Label = new DateTime(cursor.Year, cursor.Month, 1).ToString("MMM"),
					Year = cursor.Year,
					Month = cursor.Month,
					Revenue = revenue,
					Expenses = spend,
					IsInProgress = cursor.Year == today.Year && cursor.Month == today.Month,
				});
			}

			return points;
		}

		#region Helper Methods
		/// <summary>The Shared Filter Behind Both The List And Its Summary, So The Two Cannot Drift.</summary>
		private async Task<IEnumerable<ExpenseDTO>> Filtered(string? search, int? category, string? period) {
			var expenses = await unitOfWork.GetRepository<Expense>().GetAllAsync();
			if (expenses is null || !expenses.Any()) return [];

			IEnumerable<ExpenseDTO> result = mapper.Map<IEnumerable<ExpenseDTO>>(expenses);

			if (!string.IsNullOrWhiteSpace(search)) {
				var term = search.Trim();
				result = result.Where(E =>
					E.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
					(E.Vendor ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
					(E.Reference ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
					E.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
			}

			if (category is > 0) {
				var wanted = ((ExpenseCategory)category.Value).GetDisplayName();
				result = result.Where(E => E.Category == wanted);
			}

			var today = DateOnly.FromDateTime(DateTime.Now);
			var monthStart = new DateOnly(today.Year, today.Month, 1);

			result = period switch {
				"month" => result.Where(E => E.SpentOn >= monthStart),
				"last" => result.Where(E => E.SpentOn >= monthStart.AddMonths(-1) && E.SpentOn < monthStart),
				"year" => result.Where(E => E.SpentOn >= new DateOnly(today.Year, 1, 1)),
				_ => result,
			};

			return result;
		}
		#endregion
	}
}
