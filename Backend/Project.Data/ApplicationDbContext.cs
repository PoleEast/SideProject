using Microsoft.EntityFrameworkCore;
using Project.Data.Model;
using Project.Shared.Types;

namespace Project.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<StockPriceHistory> StockPriceHistories { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseShare> ExpenseShares { get; set; }


        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<CurrencyType>().HaveConversion<string>();
            configurationBuilder.Properties<StockMarketType>().HaveConversion<string>();
            configurationBuilder.Properties<TransactionType>().HaveConversion<string>();
            configurationBuilder.Properties<AvatarType>().HaveConversion<string>();
            configurationBuilder.Properties<FriendStatusType>().HaveConversion<string>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Account).HasMaxLength(32);
                entity.Property(e => e.Name).HasMaxLength(32);

                entity.HasIndex(e => e.Account).IsUnique();

                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Remark).HasMaxLength(200);

                entity.HasQueryFilter(e => e.DeletedAt == null && e.User.DeletedAt == null);

                entity.HasOne(e => e.User).WithMany(u => u.Transactions).HasForeignKey(u => u.UserId);
            });

            modelBuilder.Entity<Avatar>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.IsCurrent }).HasFilter("[IsCurrent] = 1").IsUnique();

                entity.HasQueryFilter(e => e.User.DeletedAt == null);

                entity.HasOne(e => e.User).WithMany(u => u.Avatars).HasForeignKey(u => u.UserId);
            });

            modelBuilder.Entity<StockPriceHistory>(entity =>
            {
                entity.Property(e => e.OpeningPrice).HasPrecision(18, 2);
                entity.Property(e => e.ClosingPrice).HasPrecision(18, 2);
                entity.Property(e => e.High).HasPrecision(18, 2);
                entity.Property(e => e.Low).HasPrecision(18, 2);

                entity.HasIndex(e => new { e.StockMarket, e.Code, e.Date }).IsUnique();

                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<Friend>(entity =>
            {
                entity.HasOne(e => e.OwnerUser).WithMany(u => u.Friends).HasForeignKey(e => e.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.BoundUser).WithMany().HasForeignKey(e => e.BoundUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.OwnerUserId, e.Name }).IsUnique();

                entity.HasQueryFilter(e => e.DeletedAt == null && e.OwnerUser.DeletedAt == null);
            });

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasOne(e => e.Requester).WithMany().HasForeignKey(e => e.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Addressee).WithMany().HasForeignKey(e => e.AddresseeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.RequesterId, e.AddresseeId }).IsUnique();

                entity.HasQueryFilter(e => e.DeletedAt == null && e.Requester.DeletedAt == null && e.Addressee.DeletedAt == null);
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasOne(e => e.OwnerUser).WithMany(u => u.OwnedGroups).HasForeignKey(e => e.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.DeletedAt == null && e.OwnerUser.DeletedAt == null);
            });

            modelBuilder.Entity<GroupMember>(entity =>
            {
                entity.HasOne(e => e.Group).WithMany(g => g.GroupMembers).HasForeignKey(e => e.GroupId);
                entity.HasOne(e => e.Friend).WithMany().HasForeignKey(e => e.FriendId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GroupId, e.FriendId }).IsUnique();

                entity.HasQueryFilter(e => e.DeletedAt == null && e.Group.DeletedAt == null && e.Friend.DeletedAt == null);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasOne(e => e.Group).WithMany(g => g.Expenses).HasForeignKey(e => e.GroupId);
                entity.HasOne(e => e.Payer).WithMany(gm => gm.Expenses).HasForeignKey(e => e.PayerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.DeletedAt == null && e.Group.DeletedAt == null);
            });

            modelBuilder.Entity<ExpenseShare>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.HasOne(e => e.Expense).WithMany(e => e.ExpenseShares).HasForeignKey(e => e.ExpenseId);
                entity.HasOne(e => e.GroupMember).WithMany(gm => gm.ExpenseShares).HasForeignKey(e => e.GroupMemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.DeletedAt == null && e.Expense.DeletedAt == null && e.GroupMember.DeletedAt == null);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
                var updatedAtProp = entry.Metadata.FindProperty("UpdatedAt");
                var deletedAtProp = entry.Metadata.FindProperty("DeletedAt");

                if (entry.State == EntityState.Added)
                {
                    if (createdAtProp != null)
                    {
                        entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    }

                    if (updatedAtProp != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    if (updatedAtProp != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }

                if (entry.State == EntityState.Deleted)
                {
                    if (deletedAtProp != null)
                    {
                        entry.State = EntityState.Modified;
                        entry.Property("DeletedAt").CurrentValue = DateTime.UtcNow;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
