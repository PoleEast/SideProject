using Microsoft.AspNetCore.Mvc;
using Project.Api.Common;
using Project.Core.Auth;
using Project.Shared.DTOs.Auth;
using Project.Shared.Types;

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
    }
}
