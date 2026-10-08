using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Api.Services;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using Project.Tests.Helpers;

namespace Project.Tests;

/// <summary>
/// ExpenseService 整合測試
/// 使用 InMemory 資料庫，透過服務層驗證花費的建立、列表、編輯、刪除與存取檢查
/// </summary>
/// <remarks>
/// 種子：基準幣 TWD，小明（擁有者，綁定 User）、Amy、阿華（皆未綁定）。
/// 一筆「晚餐」JPY 9,000 由小明付款三人均分，Rate 0.212345；Amy 已還小明 500。
/// </remarks>
public class ExpenseServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const int OwnerUserId = SplitBillSeeder.OwnerUserId;
    private const int GroupId = SplitBillSeeder.GroupId;
    private const int ExpenseId = SplitBillSeeder.ExpenseId;
    private const int MingMemberId = SplitBillSeeder.MingMemberId;
    private const int AmyMemberId = SplitBillSeeder.AmyMemberId;
    private const int HuaMemberId = SplitBillSeeder.HuaMemberId;

    private static ExpenseService CreateService(ApplicationDbContext context, FakeExchangeRateApiClient? exchangeRateApiClient = null)
        => new(
            context,
            new ExchangeRateService(exchangeRateApiClient ?? new FakeExchangeRateApiClient(), new MemoryCache(new MemoryCacheOptions())),
            NullLogger<ExpenseService>.Instance);

    /// <summary>
    /// 與種子的「晚餐」內容完全相同的請求，各測試再改掉要驗證的欄位
    /// </summary>
    private static ExpenseRequest DinnerRequest() => new()
    {
        Name = "晚餐",
        Description = string.Empty,
        Category = ExpenseCategoryType.Food,
        Currency = CurrencyType.JPY,
        Amount = SplitBillSeeder.ExpenseAmount,
        Date = new DateOnly(2026, 7, 1),
        PayerId = MingMemberId,
        Shares = Shares((MingMemberId, 3000m), (AmyMemberId, 3000m), (HuaMemberId, 3000m))
    };

    private static List<ExpenseShareRequest> Shares(params (int MemberId, decimal Amount)[] shares)
        => shares.Select(share => new ExpenseShareRequest { MemberId = share.MemberId, Amount = share.Amount }).ToList();

    private static async Task<ExpenseResponse> ReadDinnerAsync(ApplicationDbContext context)
    {
        var expenses = (await CreateService(context).GetExpensesAsync(GroupId, OwnerUserId)).Value!;

        return expenses.Single(expense => expense.Id == ExpenseId);
    }

    private static ExpenseRequest TaxiRequest() => new()
    {
        Name = "計程車",
        Description = string.Empty,
        Category = ExpenseCategoryType.Transport,
        Currency = CurrencyType.TWD,
        Amount = 1200m,
        Date = new DateOnly(2026, 7, 2),
        PayerId = AmyMemberId,
        Shares = Shares((MingMemberId, 400m), (AmyMemberId, 400m), (HuaMemberId, 400m))
    };

    private static ExpenseRequest RamenRequest() => new()
    {
        Name = "拉麵",
        Description = string.Empty,
        Category = ExpenseCategoryType.Food,
        Currency = CurrencyType.JPY,
        Amount = 3000m,
        Date = new DateOnly(2026, 7, 3),
        PayerId = HuaMemberId,
        Shares = Shares((MingMemberId, 1000m), (AmyMemberId, 1000m), (HuaMemberId, 1000m))
    };

    /// <summary>
    /// 經由結算服務讀取種子群組的兩兩淨額
    /// </summary>
    private static async Task<List<MemberBalance>> ReadBalancesAsync(ApplicationDbContext context)
        => (await CreateSettlementService(context).GetBalancesAsync(GroupId, OwnerUserId)).Value!;

    private static SettlementService CreateSettlementService(ApplicationDbContext context)
        => new(context, NullLogger<SettlementService>.Instance);

    private static async Task<int> CountExpensesAsync(ApplicationDbContext context)
        => (await CreateService(context).GetExpensesAsync(GroupId, OwnerUserId)).Value!.Count;

    private const int OtherGroupMemberId = 50;
    private const int OtherGroupExpenseId = 50;
    private const string IneligibleMemberMessage = "付款人與參與者必須是這個群組的現役成員";

    /// <summary>
    /// 建立另一個群組與它的一位成員，小明不是那個群組的成員
    /// </summary>
    private static async Task SeedOtherGroupAsync(ApplicationDbContext context)
    {
        const int otherUserId = 9;
        const int otherGroupId = 9;

        await SplitBillSeeder.AddUserAsync(context, otherUserId, "路人");
        context.Groups.Add(new Group
        {
            Id = otherGroupId,
            OwnerUserId = otherUserId,
            Name = "別人的群組",
            BaseCurrency = CurrencyType.TWD,
            InviteCode = "OTHER123"
        });
        context.GroupMembers.Add(new GroupMember { Id = OtherGroupMemberId, GroupId = otherGroupId, DisplayName = "路人", UserId = otherUserId });
        context.Expenses.Add(new Expense
        {
            Id = OtherGroupExpenseId,
            GroupId = otherGroupId,
            PayerId = OtherGroupMemberId,
            Name = "別人的花費",
            Category = ExpenseCategoryType.Other,
            Currency = CurrencyType.TWD,
            Amount = 100m,
            Rate = 1m,
            Date = new DateOnly(2026, 7, 1),
            CreatedByUserId = otherUserId
        });
        await context.SaveChangesAsync(Ct);
    }

    private const int DeMemberId = 4;

    /// <summary>
    /// 新增第四位現役成員「阿德」，他沒有參與任何花費
    /// </summary>
    private static async Task AddDeAsync(ApplicationDbContext context)
    {
        context.GroupMembers.Add(new GroupMember { Id = DeMemberId, GroupId = GroupId, DisplayName = "阿德" });
        await context.SaveChangesAsync(Ct);
    }

    private static Task<ActivityLog?> FindUpdateLogAsync(ApplicationDbContext context)
        => context.ActivityLogs.SingleOrDefaultAsync(log => log.ActionType == ActivityActionType.ExpenseUpdated, Ct);

    private static async Task RemoveMemberAsync(ApplicationDbContext context, int memberId)
    {
        var member = await context.GroupMembers.SingleAsync(member => member.Id == memberId, Ct);
        context.Remove(member);
        await context.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// 讓 Amy 還清對小明的欠款後，依正式的移除規則把她移除
    /// </summary>
    /// <remarks>
    /// 種子裡 Amy 欠小明 TWD 637.035、已還 TWD 500；再還 TWD 137 之後捨入為 0，才通過「淨額為 0 才可移除」。
    /// </remarks>
    private static async Task SettleAndRemoveAmyAsync(ApplicationDbContext context)
    {
        context.Settlements.Add(new Settlement
        {
            GroupId = GroupId,
            FromMemberId = AmyMemberId,
            ToMemberId = MingMemberId,
            Amount = 137m,
            Date = new DateOnly(2026, 7, 6),
            CreatedByUserId = OwnerUserId
        });
        await context.SaveChangesAsync(Ct);

        var memberService = new GroupMemberService(context, CreateSettlementService(context), NullLogger<GroupMemberService>.Instance);
        Assert.True((await memberService.RemoveMemberAsync(GroupId, AmyMemberId, OwnerUserId)).IsSuccess);
    }

    /// <summary>
    /// 把種子的「晚餐」每一個欄位都改掉的請求
    /// </summary>
    private static ExpenseRequest FullyEditedDinnerRequest()
    {
        var request = DinnerRequest();
        request.Name = "居酒屋晚餐";
        request.Description = "加點了酒";
        request.Category = ExpenseCategoryType.Entertainment;
        request.Amount = 12000m;
        request.Date = new DateOnly(2026, 7, 2);
        request.PayerId = AmyMemberId;
        request.Shares = Shares((MingMemberId, 6000m), (HuaMemberId, 6000m));

        return request;
    }

    /// <summary>
    /// 重現「後到的請求讀完之後、寫入之前，先到的請求把晚餐改名為居酒屋晚餐」
    /// </summary>
    /// <remarks>
    /// 後到的 DbContext 先把花費讀進追蹤；同一個 DbContext 之後的查詢會拿回這個已追蹤的舊實例。
    /// </remarks>
    private static async Task RenameDinnerBehindStaleContextAsync(ApplicationDbContext earlierContext, ApplicationDbContext laterContext)
    {
        await laterContext.Expenses.SingleAsync(expense => expense.Id == ExpenseId, Ct);

        var earlierRequest = DinnerRequest();
        earlierRequest.Name = "居酒屋晚餐";
        await CreateService(earlierContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, earlierRequest);
    }

    #region Rate

    [Fact(DisplayName = "建立：原幣不同於基準幣時抓取 Rate，四捨五入到 6 位後鎖入")]
    public async Task CreateExpense_ForeignCurrency_LocksRateRoundedToSixDecimals()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        exchangeRateApiClient.SetRate(CurrencyType.JPY, CurrencyType.TWD, 0.2123456789m);

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, RamenRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0.212346m, result.Value!.Rate);
        Assert.Equal(1, exchangeRateApiClient.CallCount);
    }

    [Fact(DisplayName = "建立：匯率服務失敗時回 ExternalApiError，不寫入花費")]
    public async Task CreateExpense_ExchangeRateFails_ReturnsExternalApiErrorWithoutSaving()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient { IsFailing = true };

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, RamenRequest());

        // Assert
        Assert.Equal(ResultCode.ExternalApiError, result.Code);
        Assert.Equal("目前無法取得匯率，請稍後再試", result.Message);
        Assert.Equal(1, await CountExpensesAsync(context));
    }

    [Fact(DisplayName = "建立：匯率資料中沒有基準幣時回 ExternalApiError")]
    public async Task CreateExpense_ExchangeRateLacksBaseCurrency_ReturnsExternalApiError()
    {
        // Arrange - 匯率服務有回應，但裡面沒有 JPY 對 TWD 的匯率
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        exchangeRateApiClient.SetRate(CurrencyType.JPY, CurrencyType.USD, 0.0067m);

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, RamenRequest());

        // Assert
        Assert.Equal(ResultCode.ExternalApiError, result.Code);
        Assert.Equal(1, await CountExpensesAsync(context));
    }

    [Fact(DisplayName = "編輯：原幣不變時 Rate 不變，也不呼叫匯率服務")]
    public async Task UpdateExpense_CurrencyUnchanged_KeepsRateWithoutCallingExchangeRate()
    {
        // Arrange - 匯率服務此刻的匯率與當初鎖入的不同
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        exchangeRateApiClient.SetRate(CurrencyType.JPY, CurrencyType.TWD, 0.25m);
        var request = DinnerRequest();
        request.Amount = 12000m;
        request.Shares = Shares((MingMemberId, 4000m), (AmyMemberId, 4000m), (HuaMemberId, 4000m));

        // Act
        var result = await CreateService(context, exchangeRateApiClient).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SplitBillSeeder.ExpenseRate, (await ReadDinnerAsync(context)).Rate);
        Assert.Equal(0, exchangeRateApiClient.CallCount);
    }

    [Fact(DisplayName = "編輯：原幣改為其他幣別時重新抓取 Rate，動態以一句寫出原幣與金額的變動")]
    public async Task UpdateExpense_CurrencyChangedToForeign_RefetchesRate()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        exchangeRateApiClient.SetRate(CurrencyType.USD, CurrencyType.TWD, 31.5m);
        var request = DinnerRequest();
        request.Currency = CurrencyType.USD;
        request.Amount = 90.5m;
        request.Shares = Shares((MingMemberId, 30.17m), (AmyMemberId, 30.17m), (HuaMemberId, 30.16m));

        // Act
        var result = await CreateService(context, exchangeRateApiClient).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        var dinner = await ReadDinnerAsync(context);
        Assert.Equal(CurrencyType.USD, dinner.Currency);
        Assert.Equal(31.5m, dinner.Rate);
        Assert.Equal(1, exchangeRateApiClient.CallCount);
        Assert.Equal("修改了花費「晚餐」：金額由 JPY 9,000 改為 USD 90.50、分攤金額有調整", (await FindUpdateLogAsync(context))!.Summary);
    }

    [Fact(DisplayName = "編輯：原幣改為基準幣時 Rate 為 1，不呼叫匯率服務")]
    public async Task UpdateExpense_CurrencyChangedToBase_SetsRateOneWithoutCallingExchangeRate()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        var request = DinnerRequest();
        request.Currency = CurrencyType.TWD;

        // Act
        var result = await CreateService(context, exchangeRateApiClient).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1m, (await ReadDinnerAsync(context)).Rate);
        Assert.Equal(0, exchangeRateApiClient.CallCount);
        Assert.Equal("修改了花費「晚餐」：金額由 JPY 9,000 改為 TWD 9,000", (await FindUpdateLogAsync(context))!.Summary);
    }

    [Fact(DisplayName = "編輯：改原幣時匯率服務失敗，回 ExternalApiError 且花費維持原樣")]
    public async Task UpdateExpense_CurrencyChangedAndExchangeRateFails_KeepsExpense()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient { IsFailing = true };
        var request = DinnerRequest();
        request.Currency = CurrencyType.USD;

        // Act
        var result = await CreateService(context, exchangeRateApiClient).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ExternalApiError, result.Code);
        var dinner = await ReadDinnerAsync(context);
        Assert.Equal(CurrencyType.JPY, dinner.Currency);
        Assert.Equal(SplitBillSeeder.ExpenseRate, dinner.Rate);
        Assert.Null(await FindUpdateLogAsync(context));
    }

    #endregion

    #region 驗證

    [Fact(DisplayName = "驗證：名稱去除前後空白後為空時被擋下")]
    public async Task CreateExpense_BlankName_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Name = "   ";

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("請輸入花費名稱", result.Message);
        Assert.Equal(1, await CountExpensesAsync(context));
    }

    [Theory(DisplayName = "驗證：金額為 0、為負或超過上限時被擋下")]
    [InlineData(0)]
    [InlineData(-1200)]
    [InlineData(1_000_000_000)]
    public async Task CreateExpense_AmountOutOfRange_ReturnsValidationError(int amount)
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Amount = amount;

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("金額須大於 0，且不超過 999,999,999.99", result.Message);
    }

    [Fact(DisplayName = "驗證：消費日期為 UTC 的次日時通過")]
    public async Task CreateExpense_DateIsUtcTomorrow_Succeeds()
    {
        // Arrange - 比 UTC 快的時區，當地的今天就是 UTC 的明天
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "驗證：消費日期晚於 UTC 的次日時被擋下")]
    public async Task CreateExpense_DateBeyondUtcTomorrow_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("消費日期不可為未來的日期", result.Message);
    }

    [Fact(DisplayName = "驗證：沒有參與者時被擋下")]
    public async Task CreateExpense_NoParticipants_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Shares = [];

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("請至少選擇一位參與者", result.Message);
    }

    [Fact(DisplayName = "驗證：參與者重複時被擋下")]
    public async Task CreateExpense_DuplicateParticipants_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Shares = Shares((MingMemberId, 400m), (AmyMemberId, 400m), (AmyMemberId, 400m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("參與者不可重複", result.Message);
    }

    [Fact(DisplayName = "驗證：有參與者的分攤為 0 時被擋下")]
    public async Task CreateExpense_ZeroShare_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Shares = Shares((MingMemberId, 600m), (AmyMemberId, 600m), (HuaMemberId, 0m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("每位參與者的分攤必須大於 0", result.Message);
    }

    [Fact(DisplayName = "驗證：日圓的分攤帶小數時被擋下，即使加總等於金額")]
    public async Task CreateExpense_FractionalShareInYen_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = RamenRequest();
        request.Shares = Shares((MingMemberId, 1000.5m), (AmyMemberId, 999.5m), (HuaMemberId, 1000m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("JPY 的金額與分攤必須是整數", result.Message);
    }

    [Fact(DisplayName = "驗證：美元的金額與分攤帶兩位小數時通過")]
    public async Task CreateExpense_TwoDecimalsInUsd_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        exchangeRateApiClient.SetRate(CurrencyType.USD, CurrencyType.TWD, 31.5m);
        var request = RamenRequest();
        request.Currency = CurrencyType.USD;
        request.Amount = 25.01m;
        request.Shares = Shares((MingMemberId, 8.34m), (AmyMemberId, 8.34m), (HuaMemberId, 8.33m));

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "驗證：美元的分攤帶三位小數時被擋下")]
    public async Task CreateExpense_ThreeDecimalsInUsd_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = RamenRequest();
        request.Currency = CurrencyType.USD;
        request.Amount = 25m;
        request.Shares = Shares((MingMemberId, 8.333m), (AmyMemberId, 8.333m), (HuaMemberId, 8.334m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("USD 的金額與分攤最多 2 位小數", result.Message);
    }

    [Fact(DisplayName = "驗證：分攤加總不等於金額時被擋下，且不呼叫匯率服務")]
    public async Task CreateExpense_SharesDoNotSumToAmount_ReturnsValidationErrorWithoutCallingExchangeRate()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        var request = RamenRequest();
        request.Shares = Shares((MingMemberId, 1000m), (AmyMemberId, 1000m), (HuaMemberId, 999m));

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("分攤加總必須等於花費金額", result.Message);
        Assert.Equal(0, exchangeRateApiClient.CallCount);
        Assert.Equal(1, await CountExpensesAsync(context));
    }

    #endregion

    #region 付款人與參與者的資格

    [Fact(DisplayName = "資格：指定其他群組的成員為參與者時被擋下")]
    public async Task CreateExpense_ParticipantFromOtherGroup_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);
        var request = TaxiRequest();
        request.Shares = Shares((MingMemberId, 600m), (OtherGroupMemberId, 600m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(IneligibleMemberMessage, result.Message);
    }

    [Fact(DisplayName = "資格：指定其他群組的成員為付款人時被擋下")]
    public async Task CreateExpense_PayerFromOtherGroup_ReturnsValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);
        var request = TaxiRequest();
        request.PayerId = OtherGroupMemberId;

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(IneligibleMemberMessage, result.Message);
    }

    [Fact(DisplayName = "資格：新花費指定已移除成員時被擋下，訊息與其他群組的成員相同")]
    public async Task CreateExpense_RemovedMemberAsParticipant_ReturnsSameValidationError()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await RemoveMemberAsync(context, AmyMemberId);

        // Act - 計程車的參與者含 Amy
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, TaxiRequest());

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(IneligibleMemberMessage, result.Message);
    }

    #endregion

    #region 列表

    [Fact(DisplayName = "列表：依消費日期由新到舊，同日依建立順序由新到舊")]
    public async Task GetExpenses_OrdersByDateThenIdDescending()
    {
        // Arrange - 晚餐在 7/1；計程車與超商都在 7/2，超商較晚建立
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        await service.CreateExpenseAsync(GroupId, OwnerUserId, TaxiRequest());
        var convenienceStoreRequest = TaxiRequest();
        convenienceStoreRequest.Name = "超商";
        await service.CreateExpenseAsync(GroupId, OwnerUserId, convenienceStoreRequest);

        // Act
        var result = await service.GetExpensesAsync(GroupId, OwnerUserId);

        // Assert
        Assert.Equal(["超商", "計程車", "晚餐"], result.Value!.Select(expense => expense.Name));
    }

    [Fact(DisplayName = "列表：每筆花費帶鎖入的 Rate 與完整的分攤")]
    public async Task GetExpenses_IncludesRateAndShares()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var dinner = await ReadDinnerAsync(context);

        // Assert
        Assert.Equal(SplitBillSeeder.ExpenseRate, dinner.Rate);
        Assert.Equal(MingMemberId, dinner.PayerId);
        Assert.Equal([(MingMemberId, 3000m), (AmyMemberId, 3000m), (HuaMemberId, 3000m)],
            dinner.Shares.Select(share => (share.MemberId, share.Amount)));
    }

    [Fact(DisplayName = "列表：付款人與參與者被移除後，花費仍在列表中且分攤完整")]
    public async Task GetExpenses_PayerAndParticipantRemoved_StillListsExpenseWithAllShares()
    {
        // Arrange - Amy 是計程車的付款人，也是晚餐的參與者
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        var taxi = (await service.CreateExpenseAsync(GroupId, OwnerUserId, TaxiRequest())).Value!;
        await RemoveMemberAsync(context, AmyMemberId);

        // Act
        var result = await service.GetExpensesAsync(GroupId, OwnerUserId);

        // Assert
        Assert.Equal(AmyMemberId, result.Value!.Single(expense => expense.Id == taxi.Id).PayerId);
        var dinner = result.Value!.Single(expense => expense.Id == ExpenseId);
        Assert.Contains(dinner.Shares, share => share.MemberId == AmyMemberId && share.Amount == 3000m);
    }

    #endregion

    #region 建立

    [Fact(DisplayName = "建立：原幣等於基準幣時 Rate 為 1，不呼叫匯率服務，回應含依成員排序的分攤")]
    public async Task CreateExpense_SameCurrencyAsBase_LocksRateOneWithoutCallingExchangeRate()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var exchangeRateApiClient = new FakeExchangeRateApiClient();
        var request = TaxiRequest();
        request.Shares = Shares((HuaMemberId, 400m), (MingMemberId, 400m), (AmyMemberId, 400m));

        // Act
        var result = await CreateService(context, exchangeRateApiClient).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1m, result.Value!.Rate);
        Assert.Equal(0, exchangeRateApiClient.CallCount);
        Assert.Equal(AmyMemberId, result.Value.PayerId);
        Assert.Equal([MingMemberId, AmyMemberId, HuaMemberId], result.Value.Shares.Select(share => share.MemberId));
        var expenses = (await CreateService(context).GetExpensesAsync(GroupId, OwnerUserId)).Value!;
        var stored = expenses.Single(expense => expense.Id == result.Value.Id);
        Assert.Equal(1200m, stored.Amount);
        Assert.Equal([(MingMemberId, 400m), (AmyMemberId, 400m), (HuaMemberId, 400m)],
            stored.Shares.Select(share => (share.MemberId, share.Amount)));
    }

    [Fact(DisplayName = "建立：寫入一筆指向該花費的動態，敘述含金額、付款人與參與者人數")]
    public async Task CreateExpense_WritesActivityLogTargetingExpense()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, TaxiRequest());

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.TargetExpenseId == result.Value!.Id, Ct);
        Assert.Equal(ActivityActionType.ExpenseCreated, log.ActionType);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal("新增了花費「計程車」TWD 1,200，由「Amy」付款，3 人分攤", log.Summary);
    }

    [Fact(DisplayName = "建立：名稱去除前後空白後存入")]
    public async Task CreateExpense_NameWithSurroundingWhitespace_StoresTrimmedName()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.Name = "  計程車  ";

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.Equal("計程車", result.Value!.Name);
    }

    [Fact(DisplayName = "建立：付款人不是參與者也可以建立")]
    public async Task CreateExpense_PayerNotAmongParticipants_Succeeds()
    {
        // Arrange - 小明付款，自己沒有分攤
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = TaxiRequest();
        request.PayerId = MingMemberId;
        request.Shares = Shares((AmyMemberId, 600m), (HuaMemberId, 600m));

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([AmyMemberId, HuaMemberId], result.Value!.Shares.Select(share => share.MemberId));
    }

    [Fact(DisplayName = "建立：已結束的群組照常可以建立")]
    public async Task CreateExpense_ClosedGroup_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var group = await context.Groups.SingleAsync(group => group.Id == GroupId, Ct);
        group.ClosedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(Ct);

        // Act
        var result = await CreateService(context).CreateExpenseAsync(GroupId, OwnerUserId, TaxiRequest());

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region 編輯

    [Fact(DisplayName = "編輯：以請求的內容覆蓋整筆花費，舊分攤不再有效")]
    public async Task UpdateExpense_ReplacesWholeExpense()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = FullyEditedDinnerRequest();

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        var dinner = await ReadDinnerAsync(context);
        Assert.Equal("居酒屋晚餐", dinner.Name);
        Assert.Equal("加點了酒", dinner.Description);
        Assert.Equal(ExpenseCategoryType.Entertainment, dinner.Category);
        Assert.Equal(12000m, dinner.Amount);
        Assert.Equal(new DateOnly(2026, 7, 2), dinner.Date);
        Assert.Equal(AmyMemberId, dinner.PayerId);
        Assert.Equal([(MingMemberId, 6000m), (HuaMemberId, 6000m)], dinner.Shares.Select(share => (share.MemberId, share.Amount)));
        Assert.Equal([(MingMemberId, 6000m), (HuaMemberId, 6000m)], result.Value!.Shares.Select(share => (share.MemberId, share.Amount)));
    }

    [Fact(DisplayName = "編輯：內容與現況完全相同時回成功，不寫動態")]
    public async Task UpdateExpense_NothingChanged_ReturnsSuccessWithoutActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, DinnerRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("晚餐", result.Value!.Name);
        Assert.Null(await FindUpdateLogAsync(context));
    }

    [Fact(DisplayName = "編輯：花費屬於其他群組時回 NotFound")]
    public async Task UpdateExpense_ExpenseOfOtherGroup_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, OtherGroupExpenseId, OwnerUserId, DinnerRequest());

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Equal("找不到此花費", result.Message);
    }

    [Fact(DisplayName = "編輯：驗證與建立相同，分攤加總不符時被擋下且花費維持原樣")]
    public async Task UpdateExpense_SharesDoNotSumToAmount_ReturnsValidationErrorAndKeepsExpense()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = DinnerRequest();
        request.Amount = 12000m;

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal("分攤加總必須等於花費金額", result.Message);
        Assert.Equal(SplitBillSeeder.ExpenseAmount, (await ReadDinnerAsync(context)).Amount);
    }

    [Fact(DisplayName = "編輯：動態依固定順序寫出每個變動欄位的前後值，描述只寫欄位名")]
    public async Task UpdateExpense_AllFieldsChanged_WritesActivityLogWithEveryChange()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = FullyEditedDinnerRequest();

        // Act
        await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        var log = await FindUpdateLogAsync(context);
        Assert.Equal(ExpenseId, log!.TargetExpenseId);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal(
            "修改了花費「晚餐」：名稱改為「居酒屋晚餐」、金額由 JPY 9,000 改為 JPY 12,000、付款人由「小明」改為「Amy」、"
            + "分類由餐飲改為娛樂、消費日期由 2026-07-01 改為 2026-07-02、參與者由 3 人改為 2 人、描述",
            log.Summary);
    }

    [Fact(DisplayName = "編輯：只改名稱時，動態只寫名稱")]
    public async Task UpdateExpense_OnlyNameChanged_WritesOnlyNameInActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = DinnerRequest();
        request.Name = "居酒屋晚餐";

        // Act
        await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.Equal("修改了花費「晚餐」：名稱改為「居酒屋晚餐」", (await FindUpdateLogAsync(context))!.Summary);
    }

    [Fact(DisplayName = "編輯：參與者人數不變但換了人時，動態寫「參與者有更動」")]
    public async Task UpdateExpense_ParticipantSwapped_WritesParticipantsReplaced()
    {
        // Arrange - 阿華換成阿德
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await AddDeAsync(context);
        var request = DinnerRequest();
        request.Shares = Shares((MingMemberId, 3000m), (AmyMemberId, 3000m), (DeMemberId, 3000m));

        // Act
        await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.Equal("修改了花費「晚餐」：參與者有更動", (await FindUpdateLogAsync(context))!.Summary);
    }

    [Fact(DisplayName = "編輯：參與者相同但金額不同時，動態寫「分攤金額有調整」")]
    public async Task UpdateExpense_ShareAmountsRedistributed_WritesSharesAdjusted()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var request = DinnerRequest();
        request.Shares = Shares((MingMemberId, 2000m), (AmyMemberId, 5000m), (HuaMemberId, 2000m));

        // Act
        await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.Equal("修改了花費「晚餐」：分攤金額有調整", (await FindUpdateLogAsync(context))!.Summary);
    }

    #endregion

    #region 已移除成員

    [Fact(DisplayName = "已移除成員：含已移除成員的舊花費可以只改名稱，他的分攤不變")]
    public async Task UpdateExpense_ExpenseWithRemovedParticipant_AllowsKeepingHim()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SettleAndRemoveAmyAsync(context);
        var request = DinnerRequest();
        request.Name = "居酒屋晚餐";

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Shares, share => share.MemberId == AmyMemberId && share.Amount == 3000m);
    }

    [Fact(DisplayName = "已移除成員：原本就在花費上的已移除參與者可以改為付款人")]
    public async Task UpdateExpense_RemovedParticipantBecomesPayer_Succeeds()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SettleAndRemoveAmyAsync(context);
        var request = DinnerRequest();
        request.PayerId = AmyMemberId;

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AmyMemberId, result.Value!.PayerId);
        Assert.Equal("修改了花費「晚餐」：付款人由「小明」改為「Amy」", (await FindUpdateLogAsync(context))!.Summary);
    }

    [Fact(DisplayName = "已移除成員：不能被加進他原本沒有參與的花費")]
    public async Task UpdateExpense_AddingRemovedMemberToUnrelatedExpense_ReturnsValidationError()
    {
        // Arrange - 計程車只有小明與阿華參與
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        var service = CreateService(context);
        var taxiRequest = TaxiRequest();
        taxiRequest.PayerId = MingMemberId;
        taxiRequest.Shares = Shares((MingMemberId, 600m), (HuaMemberId, 600m));
        var taxi = (await service.CreateExpenseAsync(GroupId, OwnerUserId, taxiRequest)).Value!;
        await SettleAndRemoveAmyAsync(context);
        taxiRequest.Shares = Shares((MingMemberId, 400m), (AmyMemberId, 400m), (HuaMemberId, 400m));

        // Act
        var result = await service.UpdateExpenseAsync(GroupId, taxi.Id, OwnerUserId, taxiRequest);

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(IneligibleMemberMessage, result.Message);
    }

    [Fact(DisplayName = "已移除成員：從花費移出之後加不回來")]
    public async Task UpdateExpense_ReAddingRemovedMemberAfterDroppingHim_ReturnsValidationError()
    {
        // Arrange - 先把 Amy 移出晚餐。兩次編輯是兩個請求，各用自己的 DbContext
        string databaseName = Guid.NewGuid().ToString();
        using var firstContext = DbContextTestHelper.CreateContext(databaseName);
        using var secondContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(firstContext);
        await SettleAndRemoveAmyAsync(firstContext);
        var withoutAmy = DinnerRequest();
        withoutAmy.Shares = Shares((MingMemberId, 4500m), (HuaMemberId, 4500m));
        Assert.True((await CreateService(firstContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, withoutAmy)).IsSuccess);

        // Act
        var result = await CreateService(secondContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, DinnerRequest());

        // Assert
        Assert.Equal(ResultCode.ValidationError, result.Code);
        Assert.Equal(IneligibleMemberMessage, result.Message);
    }

    [Fact(DisplayName = "已移除成員：編輯使他的淨額再度非 0 時照常成功，淨額出現在結算中")]
    public async Task UpdateExpense_MakesRemovedMemberBalanceNonZero_SucceedsAndShowsInBalances()
    {
        // Arrange - 晚餐其實是 JPY 12,000，每人 JPY 4,000
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SettleAndRemoveAmyAsync(context);
        var request = DinnerRequest();
        request.Amount = 12000m;
        request.Shares = Shares((MingMemberId, 4000m), (AmyMemberId, 4000m), (HuaMemberId, 4000m));

        // Act
        var result = await CreateService(context).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, request);

        // Assert - Amy 的分攤 JPY 4,000 × 0.212345 = TWD 849.38，扣掉已還的 TWD 637，再度欠小明 TWD 212
        Assert.True(result.IsSuccess);
        Assert.Contains(await ReadBalancesAsync(context), balance =>
            balance.DebtorMemberId == AmyMemberId && balance.CreditorMemberId == MingMemberId && balance.Amount == 212m);
    }

    [Fact(DisplayName = "已移除成員：刪除使他的淨額再度非 0 時照常成功，淨額出現在結算中")]
    public async Task DeleteExpense_MakesRemovedMemberBalanceNonZero_SucceedsAndShowsInBalances()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SettleAndRemoveAmyAsync(context);

        // Act
        var result = await CreateService(context).DeleteExpenseAsync(GroupId, ExpenseId, OwnerUserId);

        // Assert - 晚餐消失後，Amy 還過的 TWD 637 變成小明欠她的
        Assert.True(result.IsSuccess);
        var balance = Assert.Single(await ReadBalancesAsync(context));
        Assert.Equal((MingMemberId, AmyMemberId, 637m), (balance.DebtorMemberId, balance.CreditorMemberId, balance.Amount));
    }

    #endregion

    #region 存取

    [Fact(DisplayName = "存取：非成員呼叫列表、建立、編輯、刪除皆回 NotFound，資料不受影響")]
    public async Task AllOperations_CalledByNonMember_ReturnNotFound()
    {
        // Arrange
        const int strangerUserId = 2;
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SplitBillSeeder.AddUserAsync(context, strangerUserId, "路人");
        var service = CreateService(context);

        // Act
        var listResult = await service.GetExpensesAsync(GroupId, strangerUserId);
        var createResult = await service.CreateExpenseAsync(GroupId, strangerUserId, TaxiRequest());
        var updateRequest = DinnerRequest();
        updateRequest.Name = "居酒屋晚餐";
        var updateResult = await service.UpdateExpenseAsync(GroupId, ExpenseId, strangerUserId, updateRequest);
        var deleteResult = await service.DeleteExpenseAsync(GroupId, ExpenseId, strangerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, listResult.Code);
        Assert.Equal(ResultCode.NotFound, createResult.Code);
        Assert.Equal(ResultCode.NotFound, updateResult.Code);
        Assert.Equal(ResultCode.NotFound, deleteResult.Code);
        Assert.Equal("找不到此群組", listResult.Message);
        Assert.Equal("晚餐", (await ReadDinnerAsync(context)).Name);
        Assert.Equal(1, await CountExpensesAsync(context));
    }

    #endregion

    #region 刪除

    [Fact(DisplayName = "刪除：花費不再出現在列表，它的分攤也不再計入結算")]
    public async Task DeleteExpense_RemovesExpenseFromListAndBalances()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        var result = await CreateService(context).DeleteExpenseAsync(GroupId, ExpenseId, OwnerUserId);

        // Assert - 晚餐的相欠消失，只剩 Amy 還給小明的 TWD 500，變成小明欠 Amy
        Assert.True(result.IsSuccess);
        Assert.Equal(0, await CountExpensesAsync(context));
        var balance = Assert.Single(await ReadBalancesAsync(context));
        Assert.Equal((MingMemberId, AmyMemberId, 500m), (balance.DebtorMemberId, balance.CreditorMemberId, balance.Amount));
    }

    [Fact(DisplayName = "刪除：寫入一筆指向該花費的動態，敘述含名稱與金額")]
    public async Task DeleteExpense_WritesActivityLog()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);

        // Act
        await CreateService(context).DeleteExpenseAsync(GroupId, ExpenseId, OwnerUserId);

        // Assert
        var log = await context.ActivityLogs.SingleAsync(log => log.ActionType == ActivityActionType.ExpenseDeleted, Ct);
        Assert.Equal(ExpenseId, log.TargetExpenseId);
        Assert.Equal(OwnerUserId, log.ActorUserId);
        Assert.Equal("刪除了花費「晚餐」JPY 9,000", log.Summary);
    }

    [Fact(DisplayName = "刪除：花費屬於其他群組時回 NotFound")]
    public async Task DeleteExpense_ExpenseOfOtherGroup_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextTestHelper.CreateContext();
        await SplitBillSeeder.SeedAsync(context);
        await SeedOtherGroupAsync(context);

        // Act
        var result = await CreateService(context).DeleteExpenseAsync(GroupId, OtherGroupExpenseId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.NotFound, result.Code);
        Assert.Equal("找不到此花費", result.Message);
    }

    #endregion

    #region 並行

    [Fact(DisplayName = "並行：兩個請求同時編輯同一筆花費，後到者回 Conflict，花費仍是先到者寫入的內容")]
    public async Task UpdateExpense_EditedConcurrently_LaterEditReturnsConflictAndKeepsEarlierContent()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        using var readContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);
        await RenameDinnerBehindStaleContextAsync(earlierContext, laterContext);

        var laterRequest = DinnerRequest();
        laterRequest.Name = "燒肉";

        // Act
        var result = await CreateService(laterContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, laterRequest);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal("居酒屋晚餐", (await ReadDinnerAsync(readContext)).Name);
    }

    /// <remarks>
    /// 反方向「編輯撞上剛完成的刪除」無法以這個手法重現：Service 重新查詢時，已刪除的花費被 query filter 濾掉，
    /// 拿不回先前追蹤的舊實例，得到的是 NotFound。真正同時發生時由刪除推進 UpdatedAt 把關。
    /// </remarks>
    [Fact(DisplayName = "並行：刪除撞上剛完成的編輯，回 Conflict，花費仍在")]
    public async Task DeleteExpense_EditedConcurrently_ReturnsConflictAndKeepsExpense()
    {
        // Arrange
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        using var readContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);
        await RenameDinnerBehindStaleContextAsync(earlierContext, laterContext);

        // Act
        var result = await CreateService(laterContext).DeleteExpenseAsync(GroupId, ExpenseId, OwnerUserId);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal("居酒屋晚餐", (await ReadDinnerAsync(readContext)).Name);
    }

    /// <remarks>
    /// 不斷言「後到者的分攤沒有留下」：InMemory 的存檔沒有交易，新增的分攤在主表衝突之前就已寫入。
    /// SQL Server 上單次存檔即是一個交易，衝突時整批回復 —— 這一段只能靠資料庫的交易保證。
    /// </remarks>
    [Fact(DisplayName = "並行：兩個請求同時只改同一筆花費的分攤，後到者回 Conflict")]
    public async Task UpdateExpense_SharesEditedConcurrently_LaterEditReturnsConflict()
    {
        // Arrange - 兩個 DbContext 共用同一個資料庫，代表兩個同時進行的請求
        string databaseName = Guid.NewGuid().ToString();
        using var earlierContext = DbContextTestHelper.CreateContext(databaseName);
        using var laterContext = DbContextTestHelper.CreateContext(databaseName);
        await SplitBillSeeder.SeedAsync(earlierContext);

        // 後到的請求先讀到花費尚未被修改的狀態；同一個 DbContext 之後的查詢會拿回這個已追蹤的實例
        await laterContext.Expenses.SingleAsync(expense => expense.Id == ExpenseId, Ct);

        var earlierRequest = DinnerRequest();
        earlierRequest.Shares = Shares((MingMemberId, 4000m), (AmyMemberId, 3000m), (HuaMemberId, 2000m));
        await CreateService(earlierContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, earlierRequest);

        var laterRequest = DinnerRequest();
        laterRequest.Shares = Shares((MingMemberId, 5000m), (AmyMemberId, 2000m), (HuaMemberId, 2000m));

        // Act
        var result = await CreateService(laterContext).UpdateExpenseAsync(GroupId, ExpenseId, OwnerUserId, laterRequest);

        // Assert
        Assert.Equal(ResultCode.Conflict, result.Code);
        Assert.Equal("這筆花費剛被其他人修改，請重新整理後再試", result.Message);
    }

    #endregion
}
