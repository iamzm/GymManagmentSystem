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
        /// Password For The Demo Member And Trainer Logins Created Alongside The Sample Records,
        /// So Each Role Can Actually Be Signed Into. Blank Means No Demo Logins Are Created.
        /// </summary>
        public string DemoPassword { get; set; } = string.Empty;
    }
}
