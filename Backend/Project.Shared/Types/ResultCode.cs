namespace Project.Shared.Types
{
    public enum ResultCode
    {
        //成功
        Success,

        //找不到
        NotFound,

        //驗證失敗
        ValidationError,

        // 業務規則錯誤
        BusinessRuleViolation,

        // 資源有衝突
        Conflict,

        //未登入
        Unauthorized,

        //無權限
        Forbidden,

        // 第三方 API 錯誤
        ExternalApiError,

        // 伺服器內部錯誤
        InternalServerError
    }
}
