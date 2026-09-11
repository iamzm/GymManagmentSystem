using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.ExpenseDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>Admin Only: This Is The Gym's Books, Including What Everything Costs.</summary>
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class ExpensesController(IServiceManger serviceManger) : Controller {

        #region ==== Get All Expenses ====
        public async Task<IActionResult> Index(string? search, int? category, string? period) {
            var expenses = await serviceManger.ExpenseService.GetAllExpenses(search, category, period);
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Period = period;
            ViewBag.Summary = await serviceManger.ExpenseService.GetSummary(category, period);
            return View(expenses);
        }
        #endregion

        #region ==== Profit & Loss ====
        /// <summary>Revenue Against Spend, Month By Month — The View An Owner Actually Asks For.</summary>
        public async Task<IActionResult> ProfitAndLoss(int months = 6) {
            if (months is < 3 or > 24) months = 6;
            ViewBag.Months = months;
            return View(await serviceManger.ExpenseService.GetProfitTrend(months));
        }
        #endregion

        #region ==== Create Expense ====
        public IActionResult Create() => View(new CreateExpenseDTO());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(CreateExpenseDTO createExpenseDTO) {
            if (!ModelState.IsValid) return View(nameof(Create), createExpenseDTO);

            var result = await serviceManger.ExpenseService.CreateExpense(createExpenseDTO);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Saving The Expense Failed. Please Check The Values And Try Again.");
                return View(nameof(Create), createExpenseDTO);
            }

            TempData["SuccessMessage"] = $"{createExpenseDTO.Title} Was Recorded.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Edit Expense ====
        public async Task<IActionResult> Edit(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var expense = await serviceManger.ExpenseService.GetExpenseToUpdate(id);
            if (expense is null) {
                TempData["ErrorMessage"] = $"Expense With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            expense.Id = id;
            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(ExpenseToUpdateDTO expenseToUpdateDTO, int id) {
            expenseToUpdateDTO.Id = id;
            if (!ModelState.IsValid) return View(nameof(Edit), expenseToUpdateDTO);

            var result = await serviceManger.ExpenseService.UpdateExpense(expenseToUpdateDTO, id);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Expense Failed. Please Check The Values And Try Again.");
                return View(nameof(Edit), expenseToUpdateDTO);
            }

            TempData["SuccessMessage"] = "The Expense Was Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Delete Expense ====
        public async Task<IActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var expense = await serviceManger.ExpenseService.GetExpenseDetails(id);
            if (expense is null) {
                TempData["ErrorMessage"] = $"Expense With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(expense);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int id) {
            var result = await serviceManger.ExpenseService.RemoveExpense(id);

            if (!result) TempData["ErrorMessage"] = "Removing The Expense Failed.";
            else TempData["SuccessMessage"] = "The Expense Was Removed.";

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
