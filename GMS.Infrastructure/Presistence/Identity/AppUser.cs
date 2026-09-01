using Microsoft.AspNetCore.Identity;

namespace Presistence.Identity {
    /// <summary>
    /// The Login Account. It Is Deliberately Separate From The <c>Member</c> / <c>Trainer</c>
    /// Domain Entities: A Person Can Exist In The Gym Records Without Ever Signing In, And An
    /// Admin Account Has No Gym Record At All. When An Account Does Belong To Someone In The
    /// Records, <see cref="MemberId"/> Or <see cref="TrainerId"/> Points At Them.
    /// </summary>
    public class AppUser : IdentityUser {
        public string FullName { get; set; } = null!;
        public string? Photo { get; set; }
        public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public bool IsActive { get; set; } = true;

        public int? MemberId { get; set; }
        public int? TrainerId { get; set; }
    }
}
