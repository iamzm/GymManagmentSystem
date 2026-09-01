using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.PlanDTOs;

namespace GMS.MVC.Controllers {
    [Authorize]
    public class PlansController(IServiceManger serviceManger) : Controller {

        #region ==== Get Plan Details & All Plans ====
        public async Task<IActionResult> Index() {
            var plans = await serviceManger.PlanService.GetAllPlans();
            return View(plans);
        }

        public async Task<IActionResult> Details(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var plan = await serviceManger.PlanService.GetPlanById(id);
            if (plan is null) {
                TempData["ErrorMessage"] = $"Plan With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(plan);
        }
        #endregion

        #region ==== Edit ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var plan = await serviceManger.PlanService.GetPanToUpdate(id);
            if (plan is null) {
                // The Service Refuses To Hand Over A Plan That Is Deactivated Or Already Sold,
                // Because Editing It Would Rewrite Contracts Members Have Already Paid For.
                TempData["ErrorMessage"] = "This Plan Cannot Be Edited. It Is Either Deactivated Or Already Has Active Memberships.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PlanId = id;
            return View(plan);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] int id, UpdatePlanDTO updatePlanDTO) {
            if (!ModelState.IsValid) {
                ViewBag.PlanId = id;
                return View(updatePlanDTO);
            }

            var result = await serviceManger.PlanService.UpdatePlan(id, updatePlanDTO);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Plan Failed. It May Already Have Active Memberships.");
                ViewBag.PlanId = id;
                return View(updatePlanDTO);
            }

            TempData["SuccessMessage"] = $"'{updatePlanDTO.Name}' Was Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Activate & Deactivate ====
        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var result = await serviceManger.PlanService.ToggleStatus(id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "The Plan Status Was Updated."
                : "The Plan Status Could Not Be Changed While It Has Active Memberships.";

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
