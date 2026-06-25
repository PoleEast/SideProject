using Mapster;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Auth;
using Project.Shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Project.Core.Auth
{
    public class AuthService(ApplicationDbContext dbContext, ILogger<AuthService> logger)
    {
        public async Task<Result<User>> RegisterAsync(RegisterRequest request)
        {
            bool exists = await IsAccountExistsAsync(request.Account);
            if (exists)
            {
                return Result<User>.Failure(ResultCode.Conflict, "此帳號已被註冊");
            }

            string passwordHash = HashPassword(request.Password);

            var user = request.Adapt<User>();
            user.PasswordHash = passwordHash;

            try
            {
                dbContext.Add(user);
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "註冊寫入失敗。account: {Account}", request.Account);
                return Result<User>.Failure(ResultCode.InternalServerError, "資料儲存失敗");
            }

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

            try
            {
                dbContext.Update(user);
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // 更新最後登入時間屬非關鍵操作，失敗只記錄、不影響登入結果
                logger.LogError(ex, "更新最後登入時間失敗。userId: {UserId}", user.Id);
            }

            return Result<User>.Success(user);
        }

        public async Task<bool> IsAccountExistsAsync(string account) => await dbContext.Users.AnyAsync(u => u.Account == account);

        private static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        private static bool PasswordValid(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
