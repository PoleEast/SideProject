using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Auth;
using Project.Shared.Types;
using System.Net;

namespace AssetTracker.Services
{
    public class AuthService(ApplicationDbContext dbContext)
    {
        public async Task<Result<User>> RegisterAsync(RegisterRequest request)
        {
            bool exists = await IsAccountExists(request.Account);
            if (exists)
            {
                return Result<User>.Failure(ResultCode.Conflict, "此帳號已被註冊");
            }

            string passwordHash = HashPassword(request.Password);

            var user = request.Adapt<User>();
            user.PasswordHash = passwordHash;

            dbContext.Add(user);
            await dbContext.SaveChangesAsync();

            return Result<User>.Success(user);
        }

        public async Task<Result<User>> LoginAsync(LoginRequest request)
        {
            User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Account == request.Account);
            if (user == null)
            {
                return Result<User>.Failure(ResultCode.Unauthorized, "帳號或密碼錯誤");
            }

            bool isPasswordValid = PasswordValid(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Result<User>.Failure(ResultCode.Unauthorized, "帳號或密碼錯誤");
            }

            user.LastLoginAt = DateTime.UtcNow;

            dbContext.Update(user);
            await dbContext.SaveChangesAsync();

            return Result<User>.Success(user);
        }

        public async Task<bool> IsAccountExistsAsync(string account) => await dbContext.Users.AnyAsync(u => u.Account == account);

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static bool PasswordValid(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
