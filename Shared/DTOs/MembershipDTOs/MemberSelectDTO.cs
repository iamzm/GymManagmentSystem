namespace Shared.DTOs.MembershipDTOs {
    public class MemberSelectDTO {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string DisplayName => $"{Name} ({Email})";
    }
}
