using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/group/{groupId}/expenses")]
[Authorize]
public class ExpenseController(ExpenseService service) : ControllerBase
{
    /// <summary>
    /// 取得群組的所有花費，每筆含完整的分攤
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseResponse>>> GetAll(int groupId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetExpensesAsync(groupId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 建立花費
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(int groupId, ExpenseRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.CreateExpenseAsync(groupId, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 更新花費
    /// </summary>
    [HttpPut("{expenseId}")]
    public async Task<ActionResult<ExpenseResponse>> Update(int groupId, int expenseId, ExpenseRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.UpdateExpenseAsync(groupId, expenseId, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 刪除花費
    /// </summary>
    [HttpDelete("{expenseId}")]
    public async Task<ActionResult> Delete(int groupId, int expenseId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.DeleteExpenseAsync(groupId, expenseId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
