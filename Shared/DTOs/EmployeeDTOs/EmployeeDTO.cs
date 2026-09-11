namespace Shared.DTOs.EmployeeDTOs {
    public class EmployeeDTO {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string DateOfBirth { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string JobTitle { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;
        public decimal MonthlySalary { get; set; }
        public bool IsActive { get; set; }
        public DateOnly HiredOn { get; set; }

        /// <summary>Completed Years Of Service, For The Staff List.</summary>
        public int YearsOfService {
            get {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var years = today.Year - HiredOn.Year;
                if (HiredOn.AddYears(years) > today) years--;
                return years < 0 ? 0 : years;
            }
        }

        public string Initials {
            get {
                var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                return parts.Length == 1
                    ? parts[0][..1].ToUpperInvariant()
                    : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
