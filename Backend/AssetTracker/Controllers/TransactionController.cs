using AssetTracker.Services;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Project.Shared.DTOs.Transaction;
using System.Net;
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

            if (result.Code == System.Net.HttpStatusCode.Unauthorized)
            {
                return Unauthorized();
            }

            return result.Code switch
            {
                HttpStatusCode.OK => Ok(result.Result),
                HttpStatusCode.Conflict => Conflict(),
                _ => StatusCode((int)result.Code)
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
                HttpStatusCode.OK => Ok(result.Result),
                _ => StatusCode((int)result.Code)
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
                HttpStatusCode.OK => Ok(result.Result),
                HttpStatusCode.NotFound => NotFound(),
                _ => StatusCode((int)result.Code)
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
                HttpStatusCode.OK => Ok(result.Result),
                HttpStatusCode.NotFound => NotFound(),
                _ => StatusCode((int)result.Code)
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
                HttpStatusCode.OK => Ok(),
                HttpStatusCode.NotFound => NotFound(),
                _ => StatusCode((int)result.Code)
            };
        }
    }
}
