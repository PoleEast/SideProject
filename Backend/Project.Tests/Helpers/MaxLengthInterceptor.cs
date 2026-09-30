using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Project.Tests.Helpers;

/// <summary>
/// 存檔前比照 SQL Server 檢查字串長度的攔截器
/// </summary>
/// <remarks>
/// InMemory provider 不檢查字串長度，超長字串照樣存得進去。
/// 拋出 <see cref="DbUpdateException"/> 是因為 Service 捕捉的正是這個型別，測試與正式環境因此走同一條錯誤處理路徑。
/// </remarks>
public class MaxLengthInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ThrowIfTooLong(eventData.Context);
        return result;
    }

    // 非同步版預設不會轉呼叫同步版，兩條存檔路徑要各自檢查
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ThrowIfTooLong(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void ThrowIfTooLong(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var pendingEntries = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in pendingEntries)
        {
            foreach (var property in entry.Properties)
            {
                int? maxLength = property.Metadata.GetMaxLength();

                if (property.CurrentValue is string value && value.Length > maxLength)
                {
                    throw new DbUpdateException(
                        $"{entry.Metadata.DisplayName()}.{property.Metadata.Name} 長度 {value.Length} 超過上限 {maxLength}",
                        [entry]);
                }
            }
        }
    }
}
