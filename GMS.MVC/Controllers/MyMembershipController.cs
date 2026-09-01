using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.MembershipDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>
    /// A Member's Own Plan: what they are on, what they are moving to, and the switch itself.
    /// Every change here takes effect when the paid-for term ends, never mid-term.
    /// </summary>
    [Authorize]
    public class MyMembershipController(
        IServiceManger serviceManger,
        IMemberContext memberContext) : Controller {

        #region ==== My Plan ====
        public async Task<IActionResult> Index() {
            var memberId = await memberContext.GetMemberIdAsync(User);
            if (memberId is null) return View("NotLinked");

            var myMembership = await serviceManger.MembershipService.GetMyMembership(memberId.Value);
            if (myMembership is null) return View("NotLinked");

            return View(myMembership);
        }
        #endregion

        #region ==== Change Plan ====
        /// <summary>Shows what the switch would cost and when it would start, before committing.</summary>
        public async Task<IActionResult> ChangePlan(int id) {
            var memberId = await memberContext.GetMemberIdAsync(User);
            if (memberId is null) return View("NotLinked");

            var myMembership = await serviceManger.MembershipService.GetMyMembership(memberId.Value);
            if (myMembership is null) return View("NotLinked");

            var option = myMembership.Options.FirstOrDefault(O => O.PlanId == id);
            if (option is null) {
                TempData["ErrorMessage"] = "That Plan Is Not Available.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.MyMembership = myMembership;
            return View(option);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePlan(ChangePlanDTO changePlanDTO) {
            var memberId = await memberContext.GetMemberIdAsync(User);
            if (memberId is null) return View("NotLinked");

            if (!ModelState.IsValid) {
                TempData["ErrorMessage"] = "Please Choose A Plan.";
                return RedirectToAction(nameof(Index));
            }

            var (success, message) = await serviceManger.MembershipService
                .SchedulePlanChange(memberId.Value, changePlanDTO.PlanId);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Cancel A Booked Change ====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelChange() {
            var memberId = await memberContext.GetMemberIdAsync(User);
            if (memberId is null) return View("NotLinked");

            var (success, message) = await serviceManger.MembershipService.CancelScheduledChange(memberId.Value);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
