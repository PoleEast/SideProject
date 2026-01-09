using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Auth;
using System.Net;

namespace AssetTracker.Services
{
    public class AuthService(ApplicationDbContext dbContext)
    {
        public async Task<ServiceResult<User>> RegisterAsync(RegisterRequest request)
        {
            bool exists = await IsAccountExists(request.Account);
            if (exists)
            {
                return new ServiceResult<User>
                {
                    Code = HttpStatusCode.Conflict
                };
            }

            string passwordHash = HashPassword(request.Password);

            var user = request.Adapt<User>();
            user.PasswordHash = passwordHash;

            dbContext.Add(user);
            await dbContext.SaveChangesAsync();

            return new ServiceResult<User>
            {
                Code = HttpStatusCode.OK,
                Result = user
            };
        }

        public async Task<ServiceResult<User>> LoginAsync(LoginRequest request)
        {
            User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Account == request.Account);
            if (user == null)
            {
                return new ServiceResult<User>
                {
                    Code = HttpStatusCode.Conflict
                };
            }

            bool isPasswordValid = PasswordValid(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return new ServiceResult<User>
                {
                    Code = HttpStatusCode.Conflict
                };
            }

            user.LastLoginAt = DateTime.UtcNow;

            dbContext.Update(user);
            await dbContext.SaveChangesAsync();

            return new ServiceResult<User>
            {
                Code = HttpStatusCode.OK,
                Result = user
            };
        }

        public async Task<bool> IsAccountExists(string account) => await dbContext.Users.AnyAsync(u => u.Account == account);

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static bool PasswordValid(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
