using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Core.Auth;
using Project.Core.Common;
using Project.Shared.DTOs.Auth;
using Project.Shared.Types;
using System.Security.Claims;

namespace Project.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AuthService authService, JwtService jwtService) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<ActionResult> Register(RegisterRequest request)
        {
            var result = await authService.RegisterAsync(request);

            if (result.IsSuccess && result.Value != null)
            {
                var token = jwtService.GenerateToken(result.Value);
                return Ok(new { token });
            }

            return result.Code switch
            {
                ResultCode.Conflict => Conflict(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(LoginRequest request)
        {
            var result = await authService.LoginAsync(request);

            if (result.IsSuccess && result.Value != null)
            {
                var token = jwtService.GenerateToken(result.Value);
                return Ok(new { token });
            }

            return result.Code switch
            {
                ResultCode.Conflict => Conflict(result.Message),
                ResultCode.Unauthorized => Unauthorized(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }

        /// <summary>
        /// 取得呼叫者自己的資料
        /// </summary>
        [HttpGet("Me")]
        [Authorize]
        public async Task<ActionResult<MeResponse>> Me()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await authService.GetMeAsync(userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.Unauthorized => Unauthorized(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }
    }
}
