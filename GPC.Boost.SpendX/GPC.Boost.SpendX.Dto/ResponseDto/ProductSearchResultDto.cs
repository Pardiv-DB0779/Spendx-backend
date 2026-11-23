using System;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class ProductSearchResultDto
    {
        public string Sku { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Country { get; set; } = "AU";
        public decimal EverydayPrice { get; set; }
        public decimal EverydayPriceGst { get; set; }
        public string? ItemClass1 { get; set; }
        public string? ItemClass4 { get; set; }
        public decimal? AverageMonthlySales { get; set; }
        public decimal? StockOnHandStore { get; set; }
        public decimal? StockOnHandDc { get; set; }
    }
}
