using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.EmployeeDTOs;

namespace GMS.MVC.Controllers {
    /// <summary>
    /// Admin Only Throughout, Unlike Trainers Which Staff Can Browse. Every Screen Here Shows
    /// Salaries, And A Trainer Has No Business Reading Their Colleagues' Pay.
    /// </summary>
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class EmployeesController(IServiceManger serviceManger, IAttachmentService attachmentService) : Controller {

        #region ==== Get All Employees ====
        public async Task<IActionResult> Index(string? search, int? jobTitle, string? status) {
            var employees = await serviceManger.EmployeeService.GetAllEmployees(search, jobTitle, status);
            ViewBag.Search = search;
            ViewBag.JobTitle = jobTitle;
            ViewBag.Status = status;
            ViewBag.MonthlyPayroll = await serviceManger.EmployeeService.GetMonthlyPayroll();
            return View(employees);
        }
        #endregion

        #region ==== Employee Details ====
        public async Task<IActionResult> Details(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var employee = await serviceManger.EmployeeService.GetEmployeeDetails(id);
            if (employee is null) {
                TempData["ErrorMessage"] = $"Employee With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }
        #endregion

        #region ==== Create Employee ====
        public IActionResult Create() => View(new CreateEmployeeDTO());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDTO createEmployeeDTO, IFormFile? photoFile) {
            if (!ModelState.IsValid) return View(nameof(Create), createEmployeeDTO);

            var (uploaded, photoName) = await TryStorePhoto(photoFile, existingPhoto: null);
            if (!uploaded) return View(nameof(Create), createEmployeeDTO);
            createEmployeeDTO.Photo = photoName;

            var result = await serviceManger.EmployeeService.CreateEmployee(createEmployeeDTO);
            if (!result) {
                // The Save Failed, So Do Not Leave The Just-Uploaded File Orphaned On Disk.
                attachmentService.Delete(createEmployeeDTO.Photo, UploadFolders.Employees);
                createEmployeeDTO.Photo = null;
                ModelState.AddModelError(string.Empty, "Creating The Employee Failed. That Email Or Phone Number May Already Be Registered.");
                return View(nameof(Create), createEmployeeDTO);
            }

            TempData["SuccessMessage"] = $"{createEmployeeDTO.Name} Was Added To The Staff List.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Edit Employee ====
        public async Task<IActionResult> Edit(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var employee = await serviceManger.EmployeeService.GetEmployeeToUpdate(id);
            if (employee is null) {
                TempData["ErrorMessage"] = $"Employee With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.EmployeeId = id;
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EmployeeToUpdateDTO employeeToUpdateDTO, int id, IFormFile? photoFile) {
            ViewBag.EmployeeId = id;
            if (!ModelState.IsValid) return View(nameof(Edit), employeeToUpdateDTO);

            var currentPhoto = await serviceManger.EmployeeService.GetEmployeePhoto(id);
            var (uploaded, photoName) = await TryStorePhoto(photoFile, currentPhoto);
            if (!uploaded) return View(nameof(Edit), employeeToUpdateDTO);
            employeeToUpdateDTO.Photo = photoName;

            var result = await serviceManger.EmployeeService.UpdateEmployeeDetails(employeeToUpdateDTO, id);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Employee Failed. That Email Or Phone Number May Belong To Someone Else.");
                return View(nameof(Edit), employeeToUpdateDTO);
            }

            // Only Once The Save Succeeded Is The Old File Safe To Remove.
            if (!string.IsNullOrWhiteSpace(currentPhoto) && currentPhoto != photoName)
                attachmentService.Delete(currentPhoto, UploadFolders.Employees);

            TempData["SuccessMessage"] = "The Employee Record Was Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Delete Employee ====
        public async Task<IActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var employee = await serviceManger.EmployeeService.GetEmployeeDetails(id);
            if (employee is null) {
                TempData["ErrorMessage"] = $"Employee With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id) {
            var photo = await serviceManger.EmployeeService.GetEmployeePhoto(id);
            var result = await serviceManger.EmployeeService.RemoveEmployee(id);

            if (!result) {
                TempData["ErrorMessage"] = "Removing The Employee Failed.";
                return RedirectToAction(nameof(Index));
            }

            attachmentService.Delete(photo, UploadFolders.Employees);
            TempData["SuccessMessage"] = "The Employee Was Removed From The Staff List.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Helpers ====
        private async Task<(bool Stored, string? Photo)> TryStorePhoto(IFormFile? photoFile, string? existingPhoto) {
            if (photoFile is null || photoFile.Length == 0) return (true, existingPhoto);

            if (!attachmentService.IsAllowed(photoFile.FileName, photoFile.Length)) {
                ModelState.AddModelError("photoFile", "The Photo Must Be A JPG, PNG Or WEBP Image Under 2 MB.");
                return (false, existingPhoto);
            }

            await using var stream = photoFile.OpenReadStream();
            var storedName = await attachmentService.UploadAsync(stream, photoFile.FileName, UploadFolders.Employees);

            if (storedName is null) {
                ModelState.AddModelError("photoFile", "The Photo Could Not Be Saved. Please Try A Different Image.");
                return (false, existingPhoto);
            }

            return (true, storedName);
        }
        #endregion
    }
}
