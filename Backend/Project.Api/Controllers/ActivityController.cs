using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;
using Project.Core.Common;
using Project.Shared.DTOs.SplitBill;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/group/{groupId}/activities")]
[Authorize]
public class ActivityController(ActivityService service) : ControllerBase
{
    /// <summary>
    /// 取得群組動態，由新到舊；可指定只取最新的幾則
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ActivityResponse>>> GetAll(int groupId, [FromQuery] int? limit)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await service.GetActivitiesAsync(groupId, userId, limit);

        return result.Code switch
        {
            ResultCode.Success => Ok(result.Value),
            ResultCode.NotFound => NotFound(result.Message),
            _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
        };
    }
}
