using Domin.Entities;
using Domin.Enums;

namespace Domin.GymEntities {
    /// <summary>
    /// Staff Who Are Not Trainers — Reception, Management, Cleaning, Maintenance, Security.
    /// Trainers Are Deliberately Left Alone: They Are Bound To Sessions, Bookings And Their Own
    /// Login, And Folding Them In Here Would Mean Migrating Live Rows For No Functional Gain.
    /// </summary>
    public class Employee : GymUser {
        public JobTitle JobTitle { get; set; }
        public EmploymentType EmploymentType { get; set; }

        /// <summary>Gross Monthly Pay In The Configured Currency.</summary>
        public decimal MonthlySalary { get; set; }

        /// <summary>False For Someone Who Has Left. Their Record Stays For The Expense History.</summary>
        public bool IsActive { get; set; } = true;

        public string? Photo { get; set; }

        // HireDate Is Stored In BaseEntity.CreatedAt, The Same Way Trainer Does It.
    }
}
