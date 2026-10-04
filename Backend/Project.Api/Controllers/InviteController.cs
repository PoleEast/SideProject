using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

/// <remarks>
/// 呼叫者此時還不是成員，入口是邀請碼而非 groupId
/// </remarks>
[ApiController]
[Route("api/invite/{inviteCode}")]
[Authorize]
public class InviteController(InviteService service) : ControllerBase
{
    /// <summary>
    /// 以邀請碼預覽群組
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<InvitePreviewResponse>> Preview(string inviteCode)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.PreviewAsync(inviteCode, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 認領一位未綁定的既有成員
    /// </summary>
    [HttpPost("members/{memberId}/claim")]
    public async Task<ActionResult<GroupResponse>> Claim(string inviteCode, int memberId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.ClaimMemberAsync(inviteCode, memberId, userId);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }

    /// <summary>
    /// 以新成員加入
    /// </summary>
    [HttpPost("join")]
    public async Task<ActionResult<GroupResponse>> Join(string inviteCode, JoinAsNewMemberRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.JoinAsNewMemberAsync(inviteCode, userId, request);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
