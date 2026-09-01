using GMS.MVC.Models;
using GMS.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Presistence.Identity;
using System.Text;

namespace GMS.MVC.Controllers {
    [Authorize]
    public class AccountController(
        UserManager<AppUser> _userManager,
        SignInManager<AppUser> _signInManager,
        IEmailSender _emailSender,
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

        #region ==== Forgot Password ====
        [AllowAnonymous]
        public IActionResult ForgotPassword() {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(ChangePassword));
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel) {
            if (!ModelState.IsValid) return View(forgotPasswordViewModel);

            var user = await _userManager.FindByEmailAsync(forgotPasswordViewModel.Email);

            // The Confirmation Is Identical Whether Or Not The Account Exists. Saying "no such
            // account" here would turn this form into a way to discover who is registered.
            if (user is not null && user.IsActive) {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var resetUrl = Url.Action(
                    nameof(ResetPassword), "Account",
                    new { email = user.Email, token = encodedToken },
                    protocol: Request.Scheme);

                await _emailSender.SendAsync(
                    user.Email!, user.FullName,
                    "Reset your Power Fitness password",
                    BuildResetEmailHtml(user.FullName, resetUrl!),
                    BuildResetEmailText(user.FullName, resetUrl!));

                _logger.LogInformation("Password reset requested for {Email}.", user.Email);
            }
            else {
                _logger.LogInformation(
                    "Password reset requested for {Email}, which has no active account. " +
                    "The same confirmation was shown, so the form cannot be used to discover accounts.",
                    forgotPasswordViewModel.Email);
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = forgotPasswordViewModel.Email });
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation(string? email) {
            ViewBag.Email = email;
            return View();
        }
        #endregion

        #region ==== Reset Password ====
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email, string? token) {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return View("ResetPasswordInvalid");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel) {
            if (!ModelState.IsValid) return View(resetPasswordViewModel);

            var user = await _userManager.FindByEmailAsync(resetPasswordViewModel.Email);

            // A Missing Account Gets The Same Confirmation As A Successful Reset, For The Same
            // Reason As Above.
            if (user is null) return RedirectToAction(nameof(ResetPasswordConfirmation));

            string token;
            try {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordViewModel.Token));
            } catch (FormatException) {
                ModelState.AddModelError(string.Empty, "That Reset Link Is Not Valid. Please Request A New One.");
                return View(resetPasswordViewModel);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, resetPasswordViewModel.Password);

            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(resetPasswordViewModel);
            }

            // Whoever Is Resetting Has Just Proved They Own The Mailbox, And Being Locked Out Is
            // Usually Why They Are Here. Leaving The Lockout In Place Would Refuse The Password
            // They Just Set.
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("Password reset completed for {Email}.", user.Email);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();
        #endregion

        #region ==== Access Denied ====
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null) {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        #endregion

        #region ==== Reset Email Bodies ====
        private static string BuildResetEmailHtml(string name, string resetUrl) => $"""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',sans-serif;max-width:520px;color:#150f24">
              <h2 style="color:#5b21b6;margin:0 0 12px">Reset your password</h2>
              <p>Hi {name},</p>
              <p>Someone asked to reset the password for your Power Fitness account. Use the button
                 below to choose a new one. The link is single-use and expires in a day.</p>
              <p style="margin:24px 0">
                <a href="{resetUrl}"
                   style="background:#5b21b6;color:#fff;padding:12px 22px;border-radius:12px;
                          text-decoration:none;font-weight:600;display:inline-block">Choose a new password</a>
              </p>
              <p style="color:#6b6486;font-size:13px">
                If the button does not work, paste this into your browser:<br />
                <span style="word-break:break-all">{resetUrl}</span>
              </p>
              <p style="color:#6b6486;font-size:13px">
                If you did not ask for this, you can ignore this email — nothing has changed.
              </p>
            </div>
            """;

        private static string BuildResetEmailText(string name, string resetUrl) => $"""
            Hi {name},

            Someone asked to reset the password for your Power Fitness account.
            Open this link to choose a new one. It is single-use and expires in a day.

            {resetUrl}

            If you did not ask for this, you can ignore this email — nothing has changed.
            """;
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
