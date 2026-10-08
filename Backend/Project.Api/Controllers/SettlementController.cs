using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/group/{groupId}")]
[Authorize]
public class SettlementController(SettlementService service) : ControllerBase
{
    /// <summary>
    /// 取得群組的結算，即所有非零的兩兩淨額
    /// </summary>
    [HttpGet("balances")]
    public async Task<ActionResult<List<MemberBalance>>> GetBalances(int groupId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetBalancesAsync(groupId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 取得群組的所有還款
    /// </summary>
    [HttpGet("settlements")]
    public async Task<ActionResult<List<SettlementResponse>>> GetAll(int groupId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetSettlementsAsync(groupId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 記錄還款
    /// </summary>
    [HttpPost("settlements")]
    public async Task<ActionResult<SettlementResponse>> Record(int groupId, SettlementRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.RecordSettlementAsync(groupId, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 刪除還款
    /// </summary>
    [HttpDelete("settlements/{settlementId}")]
    public async Task<ActionResult> Delete(int groupId, int settlementId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.DeleteSettlementAsync(groupId, settlementId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
