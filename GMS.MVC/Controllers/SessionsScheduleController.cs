using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Abstraction.Contract;
using Shared.DTOs.BookingDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>The Weekly Timetable And The Seat Bookings Behind It.</summary>
    [Authorize]
    public class SessionsScheduleController(IServiceManger serviceManger) : Controller {

        #region ==== Weekly Timetable ====
        public async Task<IActionResult> Index(DateOnly? week) {
            var schedule = await serviceManger.BookingService.GetWeeklySchedule(week);
            return View(schedule);
        }
        #endregion

        #region ==== Session Roster ====
        // The Roster Lists Other Members By Name And Email, So It Is Staff-Only.
        [Authorize(Policy = AppPolicies.StaffOnly)]
        public async Task<IActionResult> Roster(int id) {
            var roster = await serviceManger.BookingService.GetSessionRoster(id);
            if (roster is null) {
                TempData["ErrorMessage"] = $"Session With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookableMembers = new SelectList(
                await serviceManger.BookingService.GetBookableMembersForSession(id), "Id", "DisplayName");

            return View(roster);
        }
        #endregion

        #region ==== Book & Cancel ====
        [HttpPost]
        [Authorize(Policy = AppPolicies.StaffOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(CreateBookingDTO createBookingDTO) {
            if (!ModelState.IsValid) {
                TempData["ErrorMessage"] = "Please Choose A Member To Book Into This Class.";
                return RedirectToAction(nameof(Roster), new { id = createBookingDTO.SessionId });
            }

            var (success, message) = await serviceManger.BookingService.BookSession(createBookingDTO);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Roster), new { id = createBookingDTO.SessionId });
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.StaffOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id, int sessionId) {
            var (success, message) = await serviceManger.BookingService.CancelBooking(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Roster), new { id = sessionId });
        }
        #endregion
    }
}
