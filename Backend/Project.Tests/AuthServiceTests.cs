using Microsoft.Extensions.Logging.Abstractions;
using Project.Core.Auth;
using Project.Data;
using Project.Data.Model;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// AuthService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證查詢自己的資料
/// </summary>
public class AuthServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int MingUserId = 1;
    private const string MingName = "小明";

    private static AuthService CreateService(ApplicationDbContext context)
        => new(context, NullLogger<AuthService>.Instance);

    private static async Task<User> SeedMingAsync(ApplicationDbContext context)
    {
        var ming = new User { Id = MingUserId, Account = "ming", PasswordHash = "not_used", Name = MingName };
        context.Users.Add(ming);
        await context.SaveChangesAsync(Ct);

        return ming;
    }

    #region 查詢自己的資料

    [Fact(DisplayName = "查詢自己的資料：回傳 Users 表裡的名字")]
    public async Task GetMe_ExistingUser_ReturnsNameFromUsers()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SeedMingAsync(context);
        var service = CreateService(context);

        // Act
        var result = await service.GetMeAsync(MingUserId);

        // Assert
        Assert.Equal(ResultCode.Success, result.Code);
        Assert.Equal(MingName, result.Value!.Name);
    }

    [Fact(DisplayName = "查詢自己的資料：使用者不存在時回 Unauthorized")]
    public async Task GetMe_UnknownUser_ReturnsUnauthorized()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetMeAsync(MingUserId);

        // Assert
        Assert.Equal(ResultCode.Unauthorized, result.Code);
    }

    [Fact(DisplayName = "查詢自己的資料：使用者已註銷時回 Unauthorized")]
    public async Task GetMe_DeletedUser_ReturnsUnauthorized()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        var ming = await SeedMingAsync(context);
        context.Users.Remove(ming);
        await context.SaveChangesAsync(Ct);
        var service = CreateService(context);

        // Act
        var result = await service.GetMeAsync(MingUserId);

        // Assert
        Assert.Equal(ResultCode.Unauthorized, result.Code);
    }

    #endregion
}
