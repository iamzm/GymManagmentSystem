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
            return View(dashboard);
        }
    }
}
