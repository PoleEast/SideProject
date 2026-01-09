using Microsoft.IdentityModel.Tokens;
using Project.Data.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AssetTracker.Services
{
    public class JwtService(IConfiguration configuration)
    {
        public string GenerateToken(User user)
        {
            var issuer = configuration.GetValue<string>("Jwt:Issuer");
            var signKey = configuration.GetValue<string>("Jwt:Key");
            var audience = configuration.GetValue<string>("Jwt:Audience");
            var expires = DateTime.UtcNow.AddDays(1);

            if (issuer == null || signKey == null || audience == null)
            {
                var missing = new List<string>();
                if (issuer == null) missing.Add("Issuer");
                if (signKey == null) missing.Add("Key");
                if (audience == null) missing.Add("Audience");
                var message = $"Missing JWT configuration : {string.Join(", ", missing)}";
                throw new InvalidOperationException(message);
            }

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Name, user.Name),
                new(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Sub, user.Account),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    signingCredentials: credentials,
                    expires: expires
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
