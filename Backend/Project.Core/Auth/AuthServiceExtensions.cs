using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Project.Data;
using System.Security.Claims;
using System.Text;

namespace Project.Core.Auth
{
    public static class AuthServiceExtensions
    {
        public static IServiceCollection AddSharedAuth(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<AuthService>();
            services.AddScoped<JwtService>();

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var jwtOptions = configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>() ?? new JwtOptions();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(option =>
            {
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions!.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = [jwtOptions.Audience],
                    ValidateLifetime = true,
                };

                // 效能：這是一次主鍵查詢，相較各端點原有的 DB 存取與外部 API 呼叫可以忽略。
                // 若日後成為瓶頸，可用 IMemoryCache 快取「此使用者存在」數分鐘，註銷帳號時要 cache.Remove。
                option.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                        bool userExists = int.TryParse(context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId)
                            && await dbContext.Users.AnyAsync(u => u.Id == userId);

                        if (!userExists)
                        {
                            context.Fail("使用者不存在");
                        }
                    }
                };
            });

            return services;
        }
    }
}
