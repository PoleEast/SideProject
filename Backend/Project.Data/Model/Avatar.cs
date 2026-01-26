using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Avatar
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public AvatarType Type { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
