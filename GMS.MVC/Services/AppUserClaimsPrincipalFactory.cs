using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Presistence.Identity;
using System.Security.Claims;

namespace GMS.MVC.Services {
    /// <summary>
    /// Adds the user's display name to their sign-in cookie. Without it the layout would have to
    /// hit the database on every single page render just to greet them by name.
    /// </summary>
    public class AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : UserClaimsPrincipalFactory<AppUser, IdentityRole>(userManager, roleManager, options) {

        public const string FullNameClaim = "FullName";

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user) {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(FullNameClaim, user.FullName));
            return identity;
        }
    }
}
