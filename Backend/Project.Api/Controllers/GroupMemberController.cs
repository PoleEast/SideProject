using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/group/{groupId}/members")]
[Authorize]
public class GroupMemberController(GroupMemberService service) : ControllerBase
{
    /// <summary>
    /// 取得所有成員，包含已移除者
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<GroupMemberResponse>>> GetAll(int groupId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetMembersAsync(groupId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 批次新增成員
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<List<GroupMemberResponse>>> Add(int groupId, AddGroupMembersRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.AddMembersAsync(groupId, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 修改成員的顯示名稱
    /// </summary>
    [HttpPut("{memberId}")]
    public async Task<ActionResult<GroupMemberResponse>> Rename(int groupId, int memberId, RenameGroupMemberRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.RenameMemberAsync(groupId, memberId, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 移除成員
    /// </summary>
    [HttpDelete("{memberId}")]
    public async Task<ActionResult> Remove(int groupId, int memberId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.RemoveMemberAsync(groupId, memberId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
