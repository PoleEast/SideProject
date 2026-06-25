using AssetTracker.Common;
using AssetTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Shared.DTOs.Transaction;
using Project.Shared.Types;
using System.Security.Claims;
namespace AssetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController(TransactionService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<TransactionResponse>> Create(CreateTransactionRequest request)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await service.CreateTransactionAsync(userId, request);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.Conflict => Conflict(result.Message),
                ResultCode.Unauthorized => Unauthorized(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }

        [HttpGet]
        public async Task<ActionResult<List<TransactionResponse>>> GetAll()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await service.GetUserTransactionsAsync(userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionResponse>> GetById(int id)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await service.GetByIdTransactionAsync(id, userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.NotFound => NotFound(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TransactionResponse>> Update(int id, UpdateTransactionRequest updateTransactionRequest)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await service.UpdateTransactionAsync(id, userId, updateTransactionRequest);

            return result.Code switch
            {
                ResultCode.Success => Ok(result.Value),
                ResultCode.NotFound => NotFound(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult<TransactionResponse>> Delete(int id)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Unauthorized();
            }

            var result = await service.DeleteTransactionAsync(id, userId);

            return result.Code switch
            {
                ResultCode.Success => Ok(),
                ResultCode.NotFound => NotFound(result.Message),
                _ => StatusCode(result.Code.ToHttpStatusCode(), result.Message)
            };
        }
    }
}
