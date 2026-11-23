using System.Collections.Generic;

namespace GPC.Boost.SpendX.Dto.RequestDto
{
    public class DataViewRequestDto
    {
        public int EventId { get; set; }
        public int EventOfferId { get; set; }
        public int OfferId { get => EventOfferId; set => EventOfferId = value; }
        public int OfferNo { get; set; } = 1;
        public int page { get; set; }
        public int pagePosn { get; set; }
        public int OfferTypeId { get; set; } = 18;
        public string? OfferType { get; set; }
        public bool? IsTiered { get; set; }

        // SpendX 3-Tier Thresholds and Discounts
        public decimal? Spend1 { get; set; }
        public decimal? SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }

        public decimal? SavePercent { get; set; }
        public decimal? IncrementalPercent { get; set; }
        public decimal? SpacePurchase { get; set; }

        // Pagination and Filtering
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public List<string>? SKUs { get; set; }
        public List<string>? Brands { get; set; }
        public List<string>? PartNumbers { get; set; }
        public List<string>? Descriptions { get; set; }

        // Sorting
        public bool? SortOnEdPriceDesc { get; set; }
        public bool? SortOnCatFcstDesc { get; set; }
        public bool? isOfferEdited { get; set; }
    }
}
