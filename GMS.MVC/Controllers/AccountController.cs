using GMS.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presistence.Identity;

namespace GMS.MVC.Controllers {
    [Authorize]
    public class AccountController(
        UserManager<AppUser> _userManager,
        SignInManager<AppUser> _signInManager,
        ILogger<AccountController> _logger) : Controller {

        #region ==== Login ====
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null) {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel) {
            if (!ModelState.IsValid) return View(loginViewModel);

            var user = await _userManager.FindByEmailAsync(loginViewModel.Email);
            if (user is null) {
                // The Same Message For A Missing Account And A Wrong Password, So The Form
                // Cannot Be Used To Discover Which Email Addresses Are Registered.
                ModelState.AddModelError(string.Empty, "Incorrect Email Or Password.");
                return View(loginViewModel);
            }

            if (!user.IsActive) {
                ModelState.AddModelError(string.Empty, "This Account Has Been Deactivated. Please Contact An Administrator.");
                return View(loginViewModel);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, loginViewModel.Password, loginViewModel.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut) {
                // Identity Refuses A Locked-Out Sign-In Before It Ever Checks The Password, So The
                // Right Password Fails Too. Saying How Long Is Left Stops That Looking Like A
                // Wrong Password And Sending Someone Round The Same Loop.
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var minutesLeft = lockoutEnd is null
                    ? 0
                    : (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes);

                ModelState.AddModelError(string.Empty, minutesLeft > 0
                    ? $"This Account Is Locked After Too Many Failed Attempts. Try Again In {minutesLeft} Minute{(minutesLeft == 1 ? "" : "s")} — Even The Correct Password Will Be Refused Until Then."
                    : "This Account Is Locked After Too Many Failed Attempts. Please Try Again Shortly.");

                _logger.LogWarning("Sign-in refused for {Email}: account locked for another {Minutes} minute(s).",
                    user.Email, minutesLeft);

                return View(loginViewModel);
            }

            if (!result.Succeeded) {
                ModelState.AddModelError(string.Empty, "Incorrect Email Or Password.");
                return View(loginViewModel);
            }

            _logger.LogInformation("User {Email} signed in.", user.Email);
            TempData["SuccessMessage"] = $"Welcome Back, {user.FullName}!";

            return RedirectToLocal(loginViewModel.ReturnUrl);
        }
        #endregion

        #region ==== Register ====
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null) {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel) {
            if (!ModelState.IsValid) return View(registerViewModel);

            if (await _userManager.FindByEmailAsync(registerViewModel.Email) is not null) {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "That Email Address Is Already Registered.");
                return View(registerViewModel);
            }

            var user = new AppUser {
                UserName = registerViewModel.Email,
                Email = registerViewModel.Email,
                FullName = registerViewModel.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerViewModel.Password);
            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(registerViewModel);
            }

            // Self-Registration Always Lands In The Least-Privileged Role; Staff Roles Are
            // Granted By An Administrator, Never Chosen At The Sign-Up Form.
            await _userManager.AddToRoleAsync(user, AppRoles.Member);
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = $"Welcome To Power Fitness, {user.FullName}!";
            return RedirectToLocal(registerViewModel.ReturnUrl);
        }
        #endregion

        #region ==== Logout ====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You Have Been Signed Out.";
            return RedirectToAction(nameof(Login));
        }
        #endregion

        #region ==== Profile & Password ====
        public async Task<IActionResult> Profile() {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction(nameof(Login));

            var roles = await _userManager.GetRolesAsync(user);

            return View(new ProfileViewModel {
                FullName = user.FullName,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? AppRoles.Member,
                CreatedAt = user.CreatedAt,
                MemberId = user.MemberId,
                TrainerId = user.TrainerId
            });
        }

        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel) {
            if (!ModelState.IsValid) return View(changePasswordViewModel);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(
                user, changePasswordViewModel.CurrentPassword, changePasswordViewModel.NewPassword);

            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(changePasswordViewModel);
            }

            // The Cookie Still Carries The Old Security Stamp, So Refresh It Rather Than
            // Signing The User Out Of Their Own Password Change.
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Your Password Was Updated.";
            return RedirectToAction(nameof(Profile));
        }
        #endregion

        #region ==== Access Denied ====
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null) {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        #endregion

        #region ==== Helper Method ====
        /// <summary>
        /// Only Follows A Return Url That Points Back Into This Site, So A Crafted
        /// <c>?returnUrl=</c> Cannot Turn The Login Page Into An Open Redirect.
        /// </summary>
        private IActionResult RedirectToLocal(string? returnUrl)
            => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Dashboard");
        #endregion
    }
}
