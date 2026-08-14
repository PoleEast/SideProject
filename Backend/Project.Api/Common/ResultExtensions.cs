using Project.Shared.Types;

namespace Project.Api.Common
{
    /// <summary>
    /// Result 相關的擴充方法，處理 ResultCode 與 HTTP 協定之間的對應
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// 將業務語意的 ResultCode 對應到正確的 HTTP 狀態碼
        /// </summary>
        public static int ToHttpStatusCode(this ResultCode code)
        {
            return code switch
            {
                ResultCode.Success => StatusCodes.Status200OK,
                ResultCode.NotFound => StatusCodes.Status404NotFound,
                ResultCode.ValidationError => StatusCodes.Status400BadRequest,
                ResultCode.BusinessRuleViolation => StatusCodes.Status400BadRequest,
                ResultCode.Conflict => StatusCodes.Status409Conflict,
                ResultCode.Unauthorized => StatusCodes.Status401Unauthorized,
                ResultCode.Forbidden => StatusCodes.Status403Forbidden,
                ResultCode.ExternalApiError => StatusCodes.Status502BadGateway,
                ResultCode.InternalServerError => StatusCodes.Status500InternalServerError,

                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
