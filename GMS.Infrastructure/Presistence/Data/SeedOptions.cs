namespace Presistence.Data {
    /// <summary>Bound From The <c>Seed</c> Configuration Section. Keeping The Bootstrap Admin
    /// Credentials In Configuration Means They Can Be Overridden Per Environment (Or By User
    /// Secrets) Instead Of Living In The Source Tree.</summary>
    public class SeedOptions {
        public const string SectionName = "Seed";

        public string AdminEmail { get; set; } = "admin@powerfitness.com";
        public string AdminPassword { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = "Gym Administrator";
        public bool SeedDemoData { get; set; }

        /// <summary>
        /// Destructive, And Off By Default. When Turned On, Startup Wipes The Demo-Shaped Content
        /// — Bookings, Memberships, Sessions, Members And Trainers — So <see cref="SeedDemoData"/>
        /// Can Lay Down A Fresh Set. Plans, Categories And Login Accounts Are Left Alone.
        /// Intended For Reloading Sample Data In Development, Never For A Live Database.
        /// </summary>
        public bool ResetDemoData { get; set; }

        /// <summary>
        /// Set By The Host At Startup. <see cref="ResetDemoData"/> Is Refused Unless This Is True,
        /// So A Flag Left Switched On Cannot Quietly Wipe Real Records On A Deployed Environment.
        /// </summary>
        public bool IsDevelopment { get; set; }

        /// <summary>
        /// Recovery Hatch, Off By Default. When On, Startup Resets The Administrator's Password To
        /// <see cref="AdminPassword"/>, Clears Any Lockout, And Makes Sure The Account Still Holds
        /// The Admin Role. For The Case Where A Deployment Exists But Nobody Can Sign Into It.
        /// Turn It Off Again Straight After, Or Every Restart Resets The Password.
        /// </summary>
        public bool ResetAdminPassword { get; set; }

        /// <summary>
        /// Password For The Demo Member And Trainer Logins Created Alongside The Sample Records,
        /// So Each Role Can Actually Be Signed Into. Blank Means No Demo Logins Are Created.
        /// </summary>
        public string DemoPassword { get; set; } = string.Empty;
    }
}
