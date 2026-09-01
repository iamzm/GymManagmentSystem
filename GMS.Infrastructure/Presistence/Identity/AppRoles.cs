namespace Presistence.Identity {
    /// <summary>The Three Roles The App Authorizes Against. Kept As Constants So A Typo In An
    /// <c>[Authorize(Roles = ...)]</c> Attribute Becomes A Compile Error Instead Of A Silent Lockout.</summary>
    public static class AppRoles {
        public const string Admin = "Admin";
        public const string Trainer = "Trainer";
        public const string Member = "Member";

        /// <summary>Roles Allowed Into The Back-Office Screens (Read Access To Gym Records).</summary>
        public const string Staff = Admin + "," + Trainer;

        public static readonly string[] All = [Admin, Trainer, Member];
    }
}
