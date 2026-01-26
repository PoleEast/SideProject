using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Transaction;
using Project.Shared.Types;

namespace AssetTracker.Services
{
    public class TransactionService(ApplicationDbContext dbContext)
    {
        public async Task<Result<TransactionResponse>> CreateTransactionAsync(int userId, CreateTransactionRequest request)
        {
            bool exists = await IsUserIdExists(userId);

            if (!exists)
            {
                return Result<TransactionResponse>.Failure(ResultCode.Unauthorized, "使用者不存在");
            }

            var transaction = request.Adapt<Transaction>();
            transaction.UserId = userId;

            dbContext.Add(transaction);
            await dbContext.SaveChangesAsync();

            return Result<TransactionResponse>.Success(transaction.Adapt<TransactionResponse>());
        }

        public async Task<Result<List<TransactionResponse>>> GetUserTransactionsAsync(int userId)
        {
            var transactions = await dbContext.Transactions.Where(t => t.UserId == userId).ToListAsync();

            return Result<List<TransactionResponse>>.Success(transactions.Adapt<List<TransactionResponse>>());
        }

        public async Task<Result<TransactionResponse>> GetByIdTransactionAsync(int Id, int userId)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == Id && t.UserId == userId);

            if (transaction == null)
            {
                return Result<TransactionResponse>.Failure(ResultCode.NotFound, "找不到此筆交易紀錄");
            }

            return Result<TransactionResponse>.Success(transaction.Adapt<TransactionResponse>());
        }

        public async Task<Result<TransactionResponse>> UpdateTransactionAsync(int id, int userId, UpdateTransactionRequest request)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return Result<TransactionResponse>.Failure(ResultCode.NotFound, "找不到此筆交易紀錄");
            }


            if (request.StockCode != null) transaction.StockCode = request.StockCode;
            if (request.Exchange != null) transaction.Exchange = request.Exchange;
            if (request.Date.HasValue) transaction.Date = request.Date.Value;
            if (request.Type.HasValue) transaction.Type = request.Type.Value;
            if (request.Price.HasValue) transaction.Price = request.Price.Value;
            if (request.Quantity.HasValue) transaction.Quantity = request.Quantity.Value;
            if (request.CurrencyCode != null) transaction.CurrencyCode = request.CurrencyCode;
            if (request.Remark != null) transaction.Remark = request.Remark;

            dbContext.Update(transaction);
            await dbContext.SaveChangesAsync();

            return Result<TransactionResponse>.Success(transaction.Adapt<TransactionResponse>());
        }

        public async Task<Result> DeleteTransactionAsync(int id, int userId)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return Result.Failure(ResultCode.NotFound, "找不到此筆交易紀錄");
            }

            dbContext.Transactions.Remove(transaction);
            await dbContext.SaveChangesAsync();

            return Result.Success();
        }

        private async Task<bool> IsUserIdExistsAsync(int userId) => await dbContext.Users.AnyAsync(u => u.Id == userId);
    }
}
