using System.Collections.Generic;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class DataViewResponseDto : APIResponseDto
    {
        public int EventId { get; set; }
        public int PageId { get; set; }
        public int PositionId { get; set; }
        public int OfferNo { get; set; } = 1;
        public int OfferId { get; set; }
        public int OfferTypeId { get; set; } = 18;
        public bool? IsTiered { get; set; }

        // SpendX 3-Tier Thresholds and Discounts
        public decimal Spend1 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }

        public decimal? IncrementalPercent { get; set; }
        public decimal? SpacePurchase { get; set; }

        public PaginationInfo? PaginationInfo { get; set; }
        public List<OfferSkuOverviewDto> EventOfferDataView { get; set; } = new();
    }
}
