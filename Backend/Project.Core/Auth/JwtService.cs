using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Data.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project.Core.Auth
{
    public class JwtService(IOptions<JwtOptions> jwtOptions)
    {
        private readonly JwtOptions _jwtOptions = jwtOptions.Value;

        public string GenerateToken(User user)
        {
            var expires = DateTime.UtcNow.AddDays(1);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Name, user.Name),
                new(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Sub, user.Account),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                    issuer: _jwtOptions.Issuer,
                    audience: _jwtOptions.Audience,
                    claims: claims,
                    signingCredentials: credentials,
                    expires: expires
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
