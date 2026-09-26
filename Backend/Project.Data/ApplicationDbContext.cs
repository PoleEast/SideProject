using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Project.Data.Model;
using Project.Shared.Types;

namespace Project.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            // 軟刪除靠 SaveChangesAsync 把 Deleted 改回 Modified，但 EF 預設在 Remove() 當下就處理關聯：
            // 追蹤中的子資料若是必要外鍵會拋錯，可為 null 的外鍵則被清空並寫回資料庫。
            // 延到存檔時才處理，軟刪除便搶在前面完成，EF 屆時已看不到被刪除的父實體。
            // 前提是每條關聯都明確設為 Restrict（EF 對必要外鍵的預設是 Cascade）：
            // Cascade 的子資料會在存檔時才被標為刪除，繞過軟刪除直接從資料庫消失
            ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<StockPriceHistory> StockPriceHistories { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseShare> ExpenseShares { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<CurrencyType>().HaveConversion<string>();
            configurationBuilder.Properties<StockMarketType>().HaveConversion<string>();
            configurationBuilder.Properties<TransactionType>().HaveConversion<string>();
            configurationBuilder.Properties<AvatarType>().HaveConversion<string>();
            configurationBuilder.Properties<ExpenseCategoryType>().HaveConversion<string>();
            configurationBuilder.Properties<ActivityActionType>().HaveConversion<string>();
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

                entity.HasOne(e => e.User).WithMany(u => u.Transactions).HasForeignKey(u => u.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Avatar>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.IsCurrent }).HasFilter("[IsCurrent] = 1").IsUnique();

                entity.HasQueryFilter(e => e.User.DeletedAt == null);

                entity.HasOne(e => e.User).WithMany(u => u.Avatars).HasForeignKey(u => u.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<Group>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(32);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.InviteCode).HasMaxLength(16);

                entity.HasOne<User>().WithMany(u => u.OwnedGroups).HasForeignKey(e => e.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.InviteCode).IsUnique().HasFilter("[DeletedAt] IS NULL");

                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<GroupMember>(entity =>
            {
                entity.Property(e => e.DisplayName).HasMaxLength(32);

                entity.HasOne(e => e.Group).WithMany(g => g.GroupMembers).HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL AND [DeletedAt] IS NULL");

                entity.HasQueryFilter(e => e.DeletedAt == null && e.Group.DeletedAt == null);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.Property(e => e.Rate).HasPrecision(18, 6);

                entity.HasOne(e => e.Group).WithMany(g => g.Expenses).HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Payer).WithMany(gm => gm.PaidExpenses).HasForeignKey(e => e.PayerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.GroupId);

                // 刻意不串 Payer：付款人被移除後這筆花費仍必須查得到
                entity.HasQueryFilter(e => e.DeletedAt == null && e.Group.DeletedAt == null);
            });

            modelBuilder.Entity<ExpenseShare>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.HasOne(e => e.Expense).WithMany(e => e.ExpenseShares).HasForeignKey(e => e.ExpenseId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.GroupMember).WithMany(gm => gm.ExpenseShares).HasForeignKey(e => e.GroupMemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ExpenseId);

                // 刻意不串 GroupMember：成員移除後歷史明細必須保留，
                // 串了會讓他的分攤整批消失，花費加總立刻不等於原幣總額。
                // 這不是疏漏 —— 補上去會弄壞分帳的核心不變量。
                entity.HasQueryFilter(e => e.DeletedAt == null && e.Expense.DeletedAt == null);
            });

            modelBuilder.Entity<Settlement>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.HasOne(e => e.Group).WithMany(g => g.Settlements).HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FromMember).WithMany().HasForeignKey(e => e.FromMemberId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ToMember).WithMany().HasForeignKey(e => e.ToMemberId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.GroupId);

                // 刻意不串 FromMember / ToMember：任一方被移除後這筆還款仍須留在歷史中
                entity.HasQueryFilter(e => e.DeletedAt == null && e.Group.DeletedAt == null);
            });

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.Property(e => e.Summary).HasMaxLength(500);

                entity.HasOne(e => e.Group).WithMany(g => g.ActivityLogs).HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.TargetExpense).WithMany().HasForeignKey(e => e.TargetExpenseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.GroupId);

                // 動態沒有 DeletedAt —— 它是不可變的事實紀錄，只跟著群組走
                entity.HasQueryFilter(e => e.Group.DeletedAt == null);
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
                        entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
                    }

                    if (updatedAtProp != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    if (updatedAtProp != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }

                if (entry.State == EntityState.Deleted)
                {
                    if (deletedAtProp != null)
                    {
                        entry.State = EntityState.Modified;
                        entry.Property("DeletedAt").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
