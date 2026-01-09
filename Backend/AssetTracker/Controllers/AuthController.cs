using AssetTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Project.Shared.DTOs.Auth;
using System.Net;

namespace AssetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AuthService authService, JwtService jwtService) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<ActionResult> Register(RegisterRequest request)
        {
            var result = await authService.RegisterAsync(request);

            if (result.Code == HttpStatusCode.OK && result.Result != null)
            {
                var token = jwtService.GenerateToken(result.Result);
                return Ok(new { token });
            }

            return result.Code switch
            {
                HttpStatusCode.Conflict => Conflict(),
                _ => StatusCode((int)result.Code)
            };
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(LoginRequest request)
        {
            var result = await authService.LoginAsync(request);

            if (result.Code == HttpStatusCode.OK && result.Result != null)
            {
                var token = jwtService.GenerateToken(result.Result);
                return Ok(new { token });
            }

            return result.Code switch
            {
                HttpStatusCode.Conflict => Conflict(),
                _ => StatusCode((int)result.Code)
            };
        }
    }
}
