using Project.Core.Common;
using Project.Shared.Types;

namespace Project.Tests;

/// <summary>
/// ResultExtensions 單元測試
/// 測試 ResultCode 對應到 HTTP 狀態碼的結果
/// </summary>
public class ResultExtensionsTests
{
    /// <summary>
    /// 每個 ResultCode 預期對應的 HTTP 狀態碼
    /// </summary>
    /// <remarks>
    /// 新增 ResultCode 時須在此補上一列，否則 <see cref="ExpectedStatusCodes_CoversEveryResultCode"/> 會失敗。
    /// </remarks>
    public static TheoryData<ResultCode, int> ExpectedStatusCodes => new()
    {
        { ResultCode.Success, 200 },
        { ResultCode.NotFound, 404 },
        { ResultCode.ValidationError, 400 },
        { ResultCode.BusinessRuleViolation, 400 },
        { ResultCode.Conflict, 409 },
        { ResultCode.Unauthorized, 401 },
        { ResultCode.Forbidden, 403 },
        { ResultCode.ExternalApiError, 502 },
        { ResultCode.InternalServerError, 500 }
    };

    #region ToHttpStatusCode 測試

    [Theory(DisplayName = "對應狀態碼：已定義的 ResultCode，回傳預期的 HTTP 狀態碼")]
    [MemberData(nameof(ExpectedStatusCodes))]
    public void ToHttpStatusCode_DefinedCode_ReturnsExpectedStatusCode(ResultCode code, int expectedStatusCode)
    {
        // Arrange - 輸入與預期由 ExpectedStatusCodes 提供

        // Act
        int statusCode = code.ToHttpStatusCode();

        // Assert
        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Fact(DisplayName = "對應狀態碼：未定義的列舉值，回傳 500 而不拋例外")]
    public void ToHttpStatusCode_UndefinedCode_ReturnsInternalServerError()
    {
        // Arrange
        var undefinedCode = (ResultCode)999;

        // Act
        int statusCode = undefinedCode.ToHttpStatusCode();

        // Assert
        Assert.Equal(500, statusCode);
    }

    #endregion

    #region 預期表的涵蓋範圍

    [Fact(DisplayName = "預期表：涵蓋每一個 ResultCode，新增列舉值而未補上預期時失敗")]
    public void ExpectedStatusCodes_CoversEveryResultCode()
    {
        // Arrange
        var coveredCodes = ExpectedStatusCodes.Select(row => row.Data.Item1);

        // Act
        var missingCodes = Enum.GetValues<ResultCode>().Except(coveredCodes);

        // Assert - 未定義對應的列舉值會靜默落到 500，編譯器不會提醒
        Assert.Empty(missingCodes);
    }

    #endregion
}
