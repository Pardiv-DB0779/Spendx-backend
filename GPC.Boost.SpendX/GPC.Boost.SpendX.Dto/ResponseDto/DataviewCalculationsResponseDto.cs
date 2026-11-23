namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class DataviewCalculationsResponseDto : APIResponseDto
    {
        public decimal ForecastSales { get; set; }
        public decimal ForecastCost { get; set; }
        public decimal ForecastTmDollar { get; set; }
        public decimal ForecastTmPercent { get; set; }
        public decimal TotalSupplierIncome { get; set; }
    }
}
