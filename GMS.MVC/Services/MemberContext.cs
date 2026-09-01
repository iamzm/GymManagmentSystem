using Microsoft.AspNetCore.Identity;
using Presistence.Identity;
using Services.Abstraction.Contract;
using System.Security.Claims;

namespace GMS.MVC.Services {
    /// <summary>
    /// Answers "which gym record is this signed-in person?".
    ///
    /// A login account and a gym record are deliberately separate — an admin has no gym record,
    /// and plenty of members never sign in. When the two do belong together they are matched on
    /// email, and the answer is written back to <see cref="AppUser.MemberId"/> so the lookup
    /// happens once rather than on every request.
    /// </summary>
    public class MemberContext(
        UserManager<AppUser> _userManager,
        IServiceManger _serviceManger) : IMemberContext {

        public async Task<int?> GetMemberIdAsync(ClaimsPrincipal principal) {
            var user = await _userManager.GetUserAsync(principal);
            if (user is null) return null;

            if (user.MemberId is int linked) return linked;
            if (string.IsNullOrWhiteSpace(user.Email)) return null;

            var memberId = await _serviceManger.MemberService.FindMemberIdByEmail(user.Email);
            if (memberId is null) return null;

            user.MemberId = memberId;
            await _userManager.UpdateAsync(user);
            return memberId;
        }
    }

    public interface IMemberContext {
        /// <returns>The Member Record Id For This Account, Or Null When There Is No Match.</returns>
        Task<int?> GetMemberIdAsync(ClaimsPrincipal principal);
    }
}
