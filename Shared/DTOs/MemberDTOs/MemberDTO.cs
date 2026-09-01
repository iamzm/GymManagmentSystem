namespace Shared.DTOs.MemberDTOs {
    public class MemberDTO {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public DateOnly JoinedOn { get; set; }

        /// <summary>Filled By The Service From The Member's Current Contract, When There Is One.</summary>
        public string? PlanName { get; set; }
        public DateTime? MembershipEndDate { get; set; }

        public bool IsActive => MembershipEndDate is not null && MembershipEndDate >= DateTime.Now;
        public string MembershipStatus => MembershipEndDate is null ? "No Plan" : IsActive ? "Active" : "Expired";

        /// <summary>Initials Used By The Avatar Fallback When No Photo Was Uploaded.</summary>
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
