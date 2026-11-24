using System;
using System.Collections.Generic;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class SpendXPricingExtractRowDto
    {
        public string? EventDesc { get; set; }
        public int Page { get; set; }
        public int PagePosn { get; set; }
        public string? ComOfferType { get; set; }
        public string? ComOfferIC1 { get; set; }
        public string? PartNo { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal EdPriceGst { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal Spend1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? SavePct3 { get; set; }
        public decimal AdvPriceGst { get; set; }
        public bool IsRewards { get; set; }
    }

    public class PricingExtractResponseDto : APIResponseDto
    {
        public int EventId { get; set; }
        public int TotalCount { get; set; }
        public List<SpendXPricingExtractRowDto> SpendXGetYPercent { get; set; } = new();
        public List<SpendXPricingExtractRowDto> SpendXGetYPercentLoyal { get; set; } = new();
    }
}
