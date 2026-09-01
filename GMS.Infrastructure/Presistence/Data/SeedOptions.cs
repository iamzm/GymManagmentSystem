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
    }
}
