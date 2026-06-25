using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Middleware
{
    /// <summary>
    /// 全域例外處理器，stack trace 只進 log
    /// </summary>
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // 記錄完整例外
            logger.LogError(exception,
                "未處理的例外。{Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "伺服器發生未預期的錯誤",
                Detail = "伺服器發生未預期的錯誤，請稍後再試。",
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
