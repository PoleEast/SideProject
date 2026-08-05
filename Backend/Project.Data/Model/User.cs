namespace Project.Data.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Account { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = [];
        public ICollection<Avatar> Avatars { get; set; } = [];
        public ICollection<Group> OwnedGroups { get; set; } = [];
    }
}
