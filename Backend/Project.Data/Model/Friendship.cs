using Project.Shared.Types;

namespace Project.Data.Model
{
    public class Friendship
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public int AddresseeId { get; set; }
        public FriendStatusType Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User Requester { get; set; } = null!;
        public User Addressee { get; set; } = null!;
    }
}
