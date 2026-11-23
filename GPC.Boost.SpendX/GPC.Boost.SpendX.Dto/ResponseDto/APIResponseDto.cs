using System;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class ErrorDto
    {
        public int? Status { get; set; }
        public string? Type { get; set; }
        public string? Instance { get; set; }
        public string? Title { get; set; }
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
        public string? Detail { get; set; }
        public string? InnerDetail { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class APIResponseDto
    {
        public string? Message { get; set; }
        public ErrorDto? Error { get; set; }
    }

    public class SpendXOverviewSaveResponseDto : APIResponseDto
    {
        public decimal ForecastSales { get; set; }
        public decimal ForecastCost { get; set; }
        public decimal ForecastTmDollar { get; set; }
        public decimal ForecastTmPercent { get; set; }
        public decimal TotalSupplierIncome { get; set; }
    }
}
