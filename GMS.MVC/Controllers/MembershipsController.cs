using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.Abstraction.Contract;
using Shared.DTOs.MembershipDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>Subscription Contracts: Who Is On Which Plan, Until When, And For How Much.</summary>
    [Authorize(Policy = AppPolicies.StaffOnly)]
    public class MembershipsController(IServiceManger serviceManger) : Controller {

        #region ==== List & Details ====
        public async Task<IActionResult> Index(string? search, string? status) {
            var memberships = await serviceManger.MembershipService.GetAllMemberships(search, status);
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(memberships);
        }

        public async Task<IActionResult> Details(int id) {
            var membership = await serviceManger.MembershipService.GetMembershipById(id);
            if (membership is null) {
                TempData["ErrorMessage"] = $"Membership With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }
            return View(membership);
        }
        #endregion

        #region ==== Create ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Create(int? memberId) {
            await PopulateDropdowns();
            return View(new CreateMembershipDTO {
                MemberId = memberId ?? 0,
                StartDate = DateOnly.FromDateTime(DateTime.Now)
            });
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMembershipDTO createMembershipDTO) {
            if (!ModelState.IsValid) {
                await PopulateDropdowns();
                return View(createMembershipDTO);
            }

            var (success, message) = await serviceManger.MembershipService.CreateMembership(createMembershipDTO);
            if (!success) {
                // The Service Explains Exactly Why (Plan Deactivated, Already Subscribed, …),
                // So Show That On The Form Instead Of Losing It On A Redirect.
                ModelState.AddModelError(string.Empty, message);
                await PopulateDropdowns();
                return View(createMembershipDTO);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Renew ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Renew(int id) {
            var renewal = await serviceManger.MembershipService.GetMembershipToRenew(id);
            if (renewal is null) {
                TempData["ErrorMessage"] = $"Membership With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            var membership = await serviceManger.MembershipService.GetMembershipById(id);
            ViewBag.Membership = membership;
            ViewBag.MembershipId = id;
            await PopulateDropdowns();
            return View(renewal);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew([FromRoute] int id, CreateMembershipDTO renewMembershipDTO) {
            if (!ModelState.IsValid) {
                ViewBag.Membership = await serviceManger.MembershipService.GetMembershipById(id);
                ViewBag.MembershipId = id;
                await PopulateDropdowns();
                return View(renewMembershipDTO);
            }

            var (success, message) = await serviceManger.MembershipService.RenewMembership(id, renewMembershipDTO);
            if (!success) {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Membership = await serviceManger.MembershipService.GetMembershipById(id);
                ViewBag.MembershipId = id;
                await PopulateDropdowns();
                return View(renewMembershipDTO);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Cancel ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Cancel(int id) {
            var membership = await serviceManger.MembershipService.GetMembershipById(id);
            if (membership is null) {
                TempData["ErrorMessage"] = $"Membership With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }
            return View(membership);
        }

        [HttpPost, ActionName("Cancel")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id) {
            var (success, message) = await serviceManger.MembershipService.CancelMembership(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Helper Method ====
        private async Task PopulateDropdowns() {
            var members = await serviceManger.MembershipService.GetMembersForDropdown();
            var plans = await serviceManger.MembershipService.GetActivePlansForDropdown();
            ViewBag.Members = new SelectList(members, "Id", "DisplayName");
            ViewBag.Plans = new SelectList(plans, "Id", "DisplayName");
        }
        #endregion
    }
}
