namespace Project.Data.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Account { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = [];
        public ICollection<Avatar> Avatars { get; set; } = [];
        public ICollection<Group> OwnedGroups { get; set; } = [];
    }
}
