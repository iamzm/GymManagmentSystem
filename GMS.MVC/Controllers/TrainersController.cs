using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.TrainerDTOs;

namespace GMS.MVC.Controllers {
    [Authorize(Policy = AppPolicies.StaffOnly)]
    public class TrainersController(IServiceManger serviceManger, IAttachmentService attachmentService) : Controller {

        #region ==== Get All Trainers ====
        public async Task<IActionResult> Index(string? search, int? specialty) {
            var trainers = await serviceManger.TrainerService.GetAllTrainers(search, specialty);
            ViewBag.Search = search;
            ViewBag.Specialty = specialty;
            return View(trainers);
        }
        #endregion

        #region ==== Get Trainer Details ====
        public async Task<IActionResult> TrainerDetails(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var trainer = await serviceManger.TrainerService.GetTrainerDetails(id);
            if (trainer is null) {
                TempData["ErrorMessage"] = $"Trainer With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Sessions = await serviceManger.TrainerService.GetTrainerSessions(id);
            return View(trainer);
        }
        #endregion

        #region ==== Create Trainer ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public IActionResult Create() => View(new CreateTrainerDTO());

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(CreateTrainerDTO createTrainerDTO, IFormFile? photoFile) {
            if (!ModelState.IsValid) return View(nameof(Create), createTrainerDTO);

            var (uploaded, photoName) = await TryStorePhoto(photoFile, existingPhoto: null);
            if (!uploaded) return View(nameof(Create), createTrainerDTO);
            createTrainerDTO.Photo = photoName;

            var result = await serviceManger.TrainerService.CreateTrainer(createTrainerDTO);
            if (!result) {
                // The Save Failed, So Do Not Leave The Just-Uploaded File Orphaned On Disk.
                attachmentService.Delete(createTrainerDTO.Photo, UploadFolders.Trainers);
                createTrainerDTO.Photo = null;
                ModelState.AddModelError(string.Empty, "Creating The Trainer Failed. That Email Or Phone Number May Already Be Registered.");
                return View(nameof(Create), createTrainerDTO);
            }

            TempData["SuccessMessage"] = $"{createTrainerDTO.Name} Was Added To The Trainers List.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Edit Trainer ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> EditTrainer(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var trainer = await serviceManger.TrainerService.GetTrainerToUpdate(id);
            if (trainer is null) {
                TempData["ErrorMessage"] = $"Trainer With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            trainer.Id = id;
            return View(trainer);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer([FromRoute] int id, TrainerToUpdateDTO trainerToUpdateDTO, IFormFile? photoFile) {
            trainerToUpdateDTO.Id = id;
            if (!ModelState.IsValid) return View(nameof(EditTrainer), trainerToUpdateDTO);

            var currentPhoto = await serviceManger.TrainerService.GetTrainerPhoto(id);
            trainerToUpdateDTO.Photo = currentPhoto;

            var (uploaded, photoName) = await TryStorePhoto(photoFile, currentPhoto);
            if (!uploaded) return View(nameof(EditTrainer), trainerToUpdateDTO);
            trainerToUpdateDTO.Photo = photoName;

            var result = await serviceManger.TrainerService.UpdateTrainerDetails(trainerToUpdateDTO, id);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Trainer Failed. That Email Or Phone Number May Belong To Someone Else.");
                return View(nameof(EditTrainer), trainerToUpdateDTO);
            }

            // The Record Now Points At The New File, So The Replaced One Can Go.
            if (photoFile is { Length: > 0 } && currentPhoto != trainerToUpdateDTO.Photo)
                attachmentService.Delete(currentPhoto, UploadFolders.Trainers);

            TempData["SuccessMessage"] = $"{trainerToUpdateDTO.Name}'s Details Were Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Delete Trainer ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var trainer = await serviceManger.TrainerService.GetTrainerDetails(id);
            if (trainer is null) {
                TempData["ErrorMessage"] = $"Trainer With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(trainer);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer([FromForm] int id) {
            var photo = await serviceManger.TrainerService.GetTrainerPhoto(id);
            var result = await serviceManger.TrainerService.RemoveTrainer(id);

            if (result) {
                attachmentService.Delete(photo, UploadFolders.Trainers);
                TempData["SuccessMessage"] = "The Trainer Was Deleted.";
            }
            else {
                TempData["ErrorMessage"] = "Deleting The Trainer Failed. They May Still Be Leading An Upcoming Class.";
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Helper Method ====
        /// <summary>See <c>MembersController.TryStorePhoto</c> — Same Contract, Trainers Folder.</summary>
        private async Task<(bool Stored, string? Photo)> TryStorePhoto(IFormFile? photoFile, string? existingPhoto) {
            if (photoFile is null || photoFile.Length == 0) return (true, existingPhoto);

            if (!attachmentService.IsAllowed(photoFile.FileName, photoFile.Length)) {
                ModelState.AddModelError("photoFile", "The Photo Must Be A JPG, PNG Or WEBP Image Under 2 MB.");
                return (false, existingPhoto);
            }

            await using var stream = photoFile.OpenReadStream();
            var storedName = await attachmentService.UploadAsync(stream, photoFile.FileName, UploadFolders.Trainers);

            if (storedName is null) {
                ModelState.AddModelError("photoFile", "The Photo Could Not Be Saved. Please Try A Different Image.");
                return (false, existingPhoto);
            }

            return (true, storedName);
        }
        #endregion
    }
}
