namespace Project.Shared.DTOs.Stock
{
    /// <summary>
    /// 統一的股票基本資訊DTO(從各市場API回傳轉換而來)
    /// </summary>
    public class StockInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IndustryCategory { get; set; } = string.Empty;
        public string Exchange {  get; set; } = string.Empty;
    }
}
