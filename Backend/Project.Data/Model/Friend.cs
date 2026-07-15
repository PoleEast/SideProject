namespace Project.Data.Model
{
    public class Friend
    {
        public int Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? BoundUserId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User OwnerUser { get; set; } = null!;
        public User? BoundUser { get; set; }
    }
}
