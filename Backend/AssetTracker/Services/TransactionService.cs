using Mapster;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Data.Model;
using Project.Shared.DTOs;
using Project.Shared.DTOs.Transaction;
using System.Threading.Tasks;

namespace AssetTracker.Services
{
    public class TransactionService(ApplicationDbContext dbContext)
    {
        public async Task<ServiceResult<TransactionResponse>> CreateTransactionAsync(int userId, CreateTransactionRequest request)
        {
            bool exists = await IsUserIdExists(userId);

            if (!exists)
            {
                return new ServiceResult<TransactionResponse>
                {
                    Code = System.Net.HttpStatusCode.Unauthorized,
                };
            }

            var transaction = request.Adapt<Transaction>();
            transaction.UserId = userId;

            dbContext.Add(transaction);
            await dbContext.SaveChangesAsync();

            return new ServiceResult<TransactionResponse>
            {
                Code = System.Net.HttpStatusCode.OK,
                Result = transaction.Adapt<TransactionResponse>()
            };
        }

        public async Task<ServiceResult<List<TransactionResponse>>> GetUserTransactionsAsync(int userId)
        {
            var transactions = await dbContext.Transactions.Where(t => t.UserId == userId).ToListAsync();

            return new ServiceResult<List<TransactionResponse>>
            {
                Code = System.Net.HttpStatusCode.OK,
                Result = transactions.Adapt<List<TransactionResponse>>()
            };
        }

        public async Task<ServiceResult<TransactionResponse>> GetByIdTransactionAsync(int Id, int userId)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == Id);

            if (transaction == null)
            {
                return new ServiceResult<TransactionResponse>
                {
                    Code = System.Net.HttpStatusCode.NotFound,
                };
            }

            if (transaction.UserId != userId)
            {
                return new ServiceResult<TransactionResponse>
                {
                    Code = System.Net.HttpStatusCode.NotFound,
                };
            }

            return new ServiceResult<TransactionResponse>
            {
                Code = System.Net.HttpStatusCode.OK,
                Result = transaction.Adapt<TransactionResponse>()
            };
        }

        public async Task<ServiceResult<TransactionResponse>> UpdateTransactionAsync(int id, int userId, UpdateTransactionRequest request)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return new ServiceResult<TransactionResponse>
                {
                    Code = System.Net.HttpStatusCode.NotFound
                };
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

            return new ServiceResult<TransactionResponse>
            {
                Code = System.Net.HttpStatusCode.OK,
                Result = transaction.Adapt<TransactionResponse>()
            };
        }

        public async Task<ServiceResult> DeleteTransactionAsync(int id, int userId)
        {
            var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return new ServiceResult
                {
                    Code = System.Net.HttpStatusCode.NotFound,
                };
            }

            if (transaction.UserId != userId)
            {
                return new ServiceResult
                {
                    Code = System.Net.HttpStatusCode.Unauthorized,
                };
            }

            dbContext.Transactions.Remove(transaction);
            await dbContext.SaveChangesAsync();

            return new ServiceResult()
            {
                Code = System.Net.HttpStatusCode.OK,
            };
        }

        private async Task<bool> IsUserIdExists(int userId) => await dbContext.Users.AnyAsync(u => u.Id == userId);
    }
}
