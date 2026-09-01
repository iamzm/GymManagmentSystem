using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Abstraction.Contract;
using Shared.DTOs.MemberDTOs;

namespace GMS.MVC.Controllers {
    [Authorize(Policy = AppPolicies.StaffOnly)]
    public class MembersController(IServiceManger serviceManger, IAttachmentService attachmentService) : Controller {

        #region ==== Get All Members ====
        public async Task<IActionResult> Index(string? search, string? status) {
            var members = await serviceManger.MemberService.GetAllMembers(search, status);
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(members);
        }
        #endregion

        #region ==== Get Member Details ====
        public async Task<IActionResult> MemberDetails(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var member = await serviceManger.MemberService.GetMemberDetailsById(id);
            if (member is null) {
                TempData["ErrorMessage"] = $"Member With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            member.HealthRecord = await serviceManger.MemberService.GetMemberHealthRecordDTO(id);
            ViewBag.Memberships = await serviceManger.MemberService.GetMemberMemberships(id);
            ViewBag.Bookings = await serviceManger.MemberService.GetMemberBookings(id);

            return View(member);
        }

        public async Task<IActionResult> HealthRecordDetails(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var healthRecord = await serviceManger.MemberService.GetMemberHealthRecordDTO(id);
            if (healthRecord is null) {
                TempData["ErrorMessage"] = $"Member With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            var member = await serviceManger.MemberService.GetMemberDetailsById(id);
            ViewBag.MemberName = member?.Name ?? "Member";
            ViewBag.MemberId = id;

            return View(healthRecord);
        }
        #endregion

        #region ==== Create Member ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public IActionResult Create() => View(new CreateMemberDTO());

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(CreateMemberDTO createMemberDTO, IFormFile? photoFile) {
            if (!ModelState.IsValid) return View(nameof(Create), createMemberDTO);

            var (uploaded, photoName) = await TryStorePhoto(photoFile, existingPhoto: null);
            if (!uploaded) return View(nameof(Create), createMemberDTO);
            createMemberDTO.Photo = photoName;

            var result = await serviceManger.MemberService.CreateMember(createMemberDTO);
            if (!result) {
                // The Save Failed, So Do Not Leave The Just-Uploaded File Orphaned On Disk.
                attachmentService.Delete(createMemberDTO.Photo, UploadFolders.Members);
                createMemberDTO.Photo = null;
                ModelState.AddModelError(string.Empty, "Creating The Member Failed. That Email Or Phone Number May Already Be Registered.");
                return View(nameof(Create), createMemberDTO);
            }

            TempData["SuccessMessage"] = $"{createMemberDTO.Name} Was Added To The Members List.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Edit Member ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> EditMember(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var member = await serviceManger.MemberService.GetMemberToUpdate(id);
            if (member is null) {
                TempData["ErrorMessage"] = $"Member With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            member.Id = id;
            return View(member);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember([FromRoute] int id, MemberToUpdateDTO memberToUpdateDTO, IFormFile? photoFile) {
            memberToUpdateDTO.Id = id;
            if (!ModelState.IsValid) return View(nameof(EditMember), memberToUpdateDTO);

            var currentPhoto = await serviceManger.MemberService.GetMemberPhoto(id);
            memberToUpdateDTO.Photo = currentPhoto;

            var (uploaded, photoName) = await TryStorePhoto(photoFile, currentPhoto);
            if (!uploaded) return View(nameof(EditMember), memberToUpdateDTO);
            memberToUpdateDTO.Photo = photoName;

            var result = await serviceManger.MemberService.UpdateMemberDetails(id, memberToUpdateDTO);
            if (!result) {
                ModelState.AddModelError(string.Empty, "Updating The Member Failed. That Email Or Phone Number May Belong To Someone Else.");
                return View(nameof(EditMember), memberToUpdateDTO);
            }

            // The Record Now Points At The New File, So The Replaced One Can Go.
            if (photoFile is { Length: > 0 } && currentPhoto != memberToUpdateDTO.Photo)
                attachmentService.Delete(currentPhoto, UploadFolders.Members);

            TempData["SuccessMessage"] = $"{memberToUpdateDTO.Name}'s Details Were Updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Delete Member ====
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id) {
            if (id <= 0) {
                TempData["ErrorMessage"] = "Id Can Not Be 0 Or A Negative Value.";
                return RedirectToAction(nameof(Index));
            }

            var member = await serviceManger.MemberService.GetMemberDetailsById(id);
            if (member is null) {
                TempData["ErrorMessage"] = $"Member With Id {id} Was Not Found.";
                return RedirectToAction(nameof(Index));
            }

            return View(member);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember([FromForm] int id) {
            var photo = await serviceManger.MemberService.GetMemberPhoto(id);
            var result = await serviceManger.MemberService.RemoveMember(id);

            if (result) {
                attachmentService.Delete(photo, UploadFolders.Members);
                TempData["SuccessMessage"] = "The Member Was Deleted.";
            }
            else {
                TempData["ErrorMessage"] = "Deleting The Member Failed. They May Still Be Booked Into An Upcoming Class.";
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region ==== Helper Method ====
        /// <summary>
        /// Stores An Uploaded Photo And Hands Back The File Name To Record. Returns
        /// <c>Stored: false</c> (With A Model Error Already Added) When The Upload Was Rejected,
        /// So The Caller Redisplays The Form Instead Of Saving A Member Whose Photo Silently Vanished.
        /// With No File Posted, The Existing Photo Is Kept.
        /// </summary>
        private async Task<(bool Stored, string? Photo)> TryStorePhoto(IFormFile? photoFile, string? existingPhoto) {
            if (photoFile is null || photoFile.Length == 0) return (true, existingPhoto);

            if (!attachmentService.IsAllowed(photoFile.FileName, photoFile.Length)) {
                ModelState.AddModelError("photoFile", "The Photo Must Be A JPG, PNG Or WEBP Image Under 2 MB.");
                return (false, existingPhoto);
            }

            await using var stream = photoFile.OpenReadStream();
            var storedName = await attachmentService.UploadAsync(stream, photoFile.FileName, UploadFolders.Members);

            if (storedName is null) {
                ModelState.AddModelError("photoFile", "The Photo Could Not Be Saved. Please Try A Different Image.");
                return (false, existingPhoto);
            }

            return (true, storedName);
        }
        #endregion
    }
}
