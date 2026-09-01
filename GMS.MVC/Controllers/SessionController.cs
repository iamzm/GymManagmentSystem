using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Abstraction.Contract;
using Shared.DTOs.SessionDTOs;

namespace GMS.MVC.Controllers {
    [Authorize(Policy = AppPolicies.StaffOnly)]
    public class SessionController(IServiceManger serviceManger) : Controller {

        #region ==== Get Session Details & Get All Sessions ====
        public async Task<ActionResult> Index(string? search, string? status) {
            var sessions = await serviceManger.SessionService.GetAllSessions(search, status);
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(sessions);
        }
        public async Task<ActionResult> Details(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }
            var session = await serviceManger.SessionService.GetSessionById(id);
            if (session is null) {
                TempData["ErrorMessage"] = $"Session With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }
            return View(session);
        }
        #endregion

        #region ==== Create Session ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<ActionResult> Create() {
            await GetCategoriesForDropdown();
            await GetTrainersForDropdown();
            return View();
        }

        [HttpPost] // Get DTO From Client Side Then Create The Session
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateSessionDTO createSessionDTO) {

            if (!ModelState.IsValid) {
                await GetCategoriesForDropdown();
                await GetTrainersForDropdown();
                return View(nameof(Create), createSessionDTO);
            }

            // Create The Session
            var result = await serviceManger.SessionService.CreateSession(createSessionDTO);

            if (result) {
                TempData["SuccessMessage"] = "The Session Was Created.";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError(string.Empty, "Creating The Session Failed. Check That The Start Time Is In The Future And The Capacity Is Between 1 And 25.");
                await GetCategoriesForDropdown();
                await GetTrainersForDropdown();
                return View(nameof(Create), createSessionDTO);
            }
        }
        #endregion

        #region ==== Edit Session ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<ActionResult> Edit(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }
            var session = await serviceManger.SessionService.GetSessionToUpdate(id);

            if (session is null) {
                // The Service Also Returns Null For A Session That Has Started Or Already Has Bookings.
                TempData["ErrorMessage"] = "This Session Cannot Be Edited. It Has Already Started, Finished, Or Has Bookings Against It.";
                return RedirectToAction(nameof(Index));
            }

            await GetTrainersForDropdown();
            await GetCategoriesForDropdown();

            return View(session);
        }

        [HttpPost] // Get DTO From Client Side Then Update The Session
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([FromRoute] int id, UpdateSessionDTO updateSessionDTO) {
            if (!ModelState.IsValid) {
                await GetTrainersForDropdown();
                await GetCategoriesForDropdown();
                return View(nameof(Edit), updateSessionDTO);
            }

            var result = await serviceManger.SessionService.UpdateSession(updateSessionDTO, id);

            if (result) {
                TempData["SuccessMessage"] = "The Session Was Updated.";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError(string.Empty, "Updating The Session Failed. A Session Can Only Be Changed While It Is Still Upcoming And Unbooked.");
                await GetTrainersForDropdown();
                await GetCategoriesForDropdown();
                return View(nameof(Edit), updateSessionDTO);
            }

        }
        #endregion

        #region ==== Delete Session ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<ActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }
            var session = await serviceManger.SessionService.GetSessionById(id);

            if (session is null) {
                TempData["ErrorMessage"] = $"Session With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(session);
        }

        [HttpPost] // Get DTO From Client Side Then Delete The Session
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSession([FromForm] int id) {

            var result = await serviceManger.SessionService.RemoveSession(id);

            if (result) {
                TempData["SuccessMessage"] = "The Session Was Deleted.";
            }
            else {
                TempData["ErrorMessage"] = "Deleting The Session Failed. It May Already Have Bookings Or Have Started.";
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Helper Method ====
        private async Task GetTrainersForDropdown() {
            var trainers = await serviceManger.SessionService.GetTrainersForDropdown();
            ViewBag.Trainers = new SelectList(trainers, "Id", "Name");
        } 
        private async Task GetCategoriesForDropdown() {
            var categories = await serviceManger.SessionService.GetCategoriesForDropdown();
            ViewBag.Categories = new SelectList(categories, "Id", "CategoryName");
        } 
        #endregion
    }
}
