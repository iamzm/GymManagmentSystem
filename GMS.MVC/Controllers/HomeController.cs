using GMS.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using System.Diagnostics;

namespace GMS.MVC.Controllers {
    /// <summary>The Public Face Of The Site: The Marketing Landing Page And The Error Screens.</summary>
    [AllowAnonymous]
    public class HomeController(IServiceManger serviceManger) : Controller {

        public async Task<IActionResult> Index() {
            // Signed-In Staff Get The Working Dashboard Rather Than The Marketing Page.
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");

            // The Landing Page Advertises The Real Gym: Live Counters, The Plans Actually On
            // Sale, And The Classes Genuinely Coming Up This Week.
            var schedule = await serviceManger.BookingService.GetWeeklySchedule();
            var plans = await serviceManger.PlanService.GetAllPlans();
            var trainers = await serviceManger.TrainerService.GetAllTrainers();

            return View(new LandingViewModel {
                Stats = await serviceManger.AnalyticsService.GetAnalyticData(),
                Plans = [.. plans.Where(P => P.IsActive).OrderBy(P => P.Price).Take(3)],
                UpcomingClasses = [.. schedule.Days
                    .SelectMany(D => D.Slots)
                    .Where(S => S.Status != "Completed")
                    .OrderBy(S => S.StartDate)
                    .Take(3)],
                Trainers = [.. trainers.Take(4)]
            });
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>Friendly Pages For The Status Codes The Pipeline Re-Executes Into (404, 403, …).</summary>
        [Route("Home/StatusCode")]
        public IActionResult StatusCodeHandler(int? code) {
            ViewBag.Code = code ?? 404;
            return View("StatusCode");
        }
    }
}
