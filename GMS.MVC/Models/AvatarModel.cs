namespace GMS.MVC.Models {
    /// <summary>Feeds the shared avatar partial: a stored photo when there is one, initials otherwise.</summary>
    public class AvatarModel {
        public string Name { get; set; } = "?";
        public string? Photo { get; set; }
        public string Folder { get; set; } = "members";
        public string Size { get; set; } = "md";

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
