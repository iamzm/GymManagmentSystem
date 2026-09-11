using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.InventoryDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>
    /// Staff Can See And Adjust Stock — A Receptionist Handing Out Towels Needs To Log It —
    /// But Only An Admin Can Add, Edit Or Remove Items, Because Those Carry Purchase Costs.
    /// </summary>
    [Authorize(Policy = AppPolicies.StaffOnly)]
    public class InventoryController(IServiceManger serviceManger) : Controller {

        #region ==== Get All Items ====
        public async Task<IActionResult> Index(string? search, int? kind, string? flag) {
            var items = await serviceManger.InventoryService.GetAllItems(search, kind, flag);
            ViewBag.Search = search;
            ViewBag.Kind = kind;
            ViewBag.Flag = flag;
            ViewBag.Summary = await serviceManger.InventoryService.GetSummary();
            return View(items);
        }
        #endregion

        #region ==== Item Details ====
        public async Task<IActionResult> Details(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var item = await serviceManger.InventoryService.GetItemDetails(id);
            if (item is null) {
                TempData["ErrorMessage"] = $"Item With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }
        #endregion

        #region ==== Create Item ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public IActionResult Create() => View(new CreateInventoryItemDTO());

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(CreateInventoryItemDTO createItemDTO) {
            if (!ModelState.IsValid) return View(nameof(Create), createItemDTO);

            var result = await serviceManger.InventoryService.CreateItem(createItemDTO);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Saving The Item Failed. Please Check The Values And Try Again.");
                return View(nameof(Create), createItemDTO);
            }

            TempData["SuccessMessage"] = $"{createItemDTO.Name} Was Added To The Inventory.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Edit Item ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Edit(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var item = await serviceManger.InventoryService.GetItemToUpdate(id);
            if (item is null) {
                TempData["ErrorMessage"] = $"Item With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            item.Id = id;
            return View(item);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(InventoryItemToUpdateDTO itemToUpdateDTO, int id) {
            itemToUpdateDTO.Id = id;
            if (!ModelState.IsValid) return View(nameof(Edit), itemToUpdateDTO);

            var result = await serviceManger.InventoryService.UpdateItem(itemToUpdateDTO, id);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Item Failed. Please Check The Values And Try Again.");
                return View(nameof(Edit), itemToUpdateDTO);
            }

            TempData["SuccessMessage"] = "The Item Was Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Adjust Stock ====
        /// <summary>
        /// The Quick +/- On The List. Staff Can Use It Because Recording That Six Towels Went
        /// Out Is Part Of Running The Floor, Not A Financial Change.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int delta, string? returnFlag) {
            var result = await serviceManger.InventoryService.AdjustQuantity(id, delta);

            if (!result) TempData["ErrorMessage"] = "That Adjustment Would Take The Count Below Zero.";
            else TempData["SuccessMessage"] = $"Stock Adjusted By {delta:+#;-#;0}.";

            return RedirectToAction(nameof(Index), new { flag = returnFlag });
        }
        #endregion

        #region ==== Delete Item ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var item = await serviceManger.InventoryService.GetItemDetails(id);
            if (item is null) {
                TempData["ErrorMessage"] = $"Item With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int id) {
            var result = await serviceManger.InventoryService.RemoveItem(id);

            if (!result) TempData["ErrorMessage"] = "Removing The Item Failed.";
            else TempData["SuccessMessage"] = "The Item Was Removed From The Inventory.";

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
