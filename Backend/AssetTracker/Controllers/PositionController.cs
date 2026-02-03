using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Shared.DTOs.Position;
using Project.Shared.Types;
using System.Security.Claims;

namespace AssetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionController(PositionService positionService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<PositionResponse>>> GetPositions()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await positionService.GetPositionAsync(userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                _ => StatusCode((int)result.Code)
            };
        }

        [HttpGet("realized-pnl")]
        public async Task<ActionResult<List<RealizedPnLResponse>>> GetRealizedPnL()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await positionService.GetRealizedPnLAsync(userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.BusinessRuleViolation => BadRequest(result.Message),
                _ => StatusCode((int)result.Code)
            };
        }
    }
}
