namespace GMS.MVC.Models {
    public class ProfileViewModel {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateOnly CreatedAt { get; set; }
        public int? MemberId { get; set; }
        public int? TrainerId { get; set; }

        public string Initials {
            get {
                var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                return parts.Length == 1
                    ? parts[0][..1].ToUpperInvariant()
                    : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }
}
