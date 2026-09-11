using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presistence.Identity;
using Services.Abstraction.Contract;

namespace GMS.MVC.Controllers {
    /// <summary>The Signed-In Home Screen: Live Gym Metrics, Renewals Due And The Week Ahead.</summary>
    [Authorize]
    public class DashboardController(IServiceManger serviceManger) : Controller {

        public async Task<IActionResult> Index() {
            // Members Have No Back-Office Numbers To See; Send Them Straight To The Timetable.
            if (!User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Trainer))
                return RedirectToAction("Index", "SessionsSchedule");

            var dashboard = await serviceManger.AnalyticsService.GetDashboardData();

            // The Operations Panels Are Composed Here Rather Than Inside AnalyticsService, So The
            // Profit Maths Lives In Exactly One Place — ExpenseService — Instead Of Being Repeated.
            // Only An Admin Sees Money And Payroll; A Trainer Gets The Gym Numbers Without The Books.
            if (User.IsInRole(AppRoles.Admin)) {
                dashboard.ProfitTrend = [.. await serviceManger.ExpenseService.GetProfitTrend(6)];
                dashboard.MonthlyPayroll = await serviceManger.EmployeeService.GetMonthlyPayroll();
            }

            dashboard.LowStock = [.. await serviceManger.InventoryService.GetLowStock(4)];
            dashboard.ServiceDue = [.. await serviceManger.InventoryService.GetServiceDue(4)];

            return View(dashboard);
        }
    }
}
