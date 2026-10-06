using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController(GroupService service) : ControllerBase
{
    /// <summary>
    /// 取得呼叫者參與的所有群組
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GroupResponse>>> GetAll()
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetMyGroupsAsync(userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 取得單一群組
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponse>> GetById(int id)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetGroupByIdAsync(id, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 建立群組
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> Create(CreateGroupRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.CreateGroupAsync(userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.Unauthorized => Unauthorized(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 重置邀請碼
    /// </summary>
    [HttpPost("{id}/invite-code")]
    public async Task<ActionResult<GroupResponse>> ResetInviteCode(int id)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.ResetInviteCodeAsync(id, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 更新群組的名稱、描述與基準幣
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<GroupResponse>> Update(int id, UpdateGroupRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.UpdateGroupAsync(id, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 設定已結束狀態
    /// </summary>
    [HttpPut("{id}/closed")]
    public async Task<ActionResult<GroupResponse>> SetClosed(int id, SetGroupClosedRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.SetGroupClosedAsync(id, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 刪除群組
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.DeleteGroupAsync(id, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
