using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TEventOffer
    {
        public int OfferId { get; set; }
        public int OfferNumber { get; set; } = 1;
        public int EventId { get; set; } = 1;
        
        // Layout position in flyer grid
        public int Page { get; set; } = 1;
        public int PagePosition { get; set; } = 1;
        public string? Status { get; set; } = "Draft";

        // Offer header details
        public int OfferTypeId { get; set; } = 18;
        public string OfferType { get; set; } = "SPEND X GET Y% OFF";
        public string? CommercialOfferType { get; set; }
        public string? CommercialOfferItemClass1 { get; set; }
        public string? CommercialCategoryManager { get; set; }
        public string? OfferName { get; set; }
        public string? OfferDescription { get; set; }
        public string? Brand { get; set; }
        public string Country { get; set; } = "AU";
        public string? CategoryCostType { get; set; } = "NAT AVG COST";
        public bool? IsRewards { get; set; } = false;

        // SpendX 3-Tier Thresholds and Discounts
        public decimal Spend1 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }

        // Legacy Procedure Threshold Fields
        public decimal? ThresholdAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }

        // Planning Inputs
        public decimal? IncrementalPercentage { get; set; } = 0;
        public decimal? SpacePurchase { get; set; } = 0;

        // Forecast Metrics & Header Rollups
        public int? ForecastUnits { get; set; } = 0;
        public decimal? EverydayUnits { get; set; } = 0;
        public int? IncrementalUnits { get; set; } = 0;
        public decimal? ForecastCost { get; set; } = 0;
        public decimal? ForecastSales { get; set; } = 0;
        public decimal? ForecastTradeMargin { get; set; } = 0;
        public decimal? ForecastTradeMarginDollar { get; set; } = 0;
        public decimal? ForecastTmPercent { get; set; } = 0;
        public decimal? ForecastTradeMarginPercent { get; set; } = 0;
        public decimal? IncrementalSales { get; set; } = 0;
        public decimal? IncrementalSalesDollar { get; set; } = 0;
        public decimal? IncrementalTm { get; set; } = 0;
        public decimal? IncrementalTmDollar { get; set; } = 0;
        public decimal? TotalSupplierIncome { get; set; } = 0;
        public decimal? TotalScanSupportDollar { get; set; } = 0;
        public decimal? TotalScanSupportPercent { get; set; } = 0;

        // Pricing Summary Fields
        public decimal? EverydayPrice { get; set; }
        public decimal? EverydayPriceGst { get; set; }
        public decimal? AdvertisedPrice { get; set; }
        public decimal? AdvertisedPriceGst { get; set; }
        public decimal? SaveValue { get; set; }
        public decimal? SavePercent { get; set; }
        public decimal? CalculatedSavePercent { get; set; }
        public decimal? CalculatedSaveValue { get; set; }

        // Merchandising Callouts / Promo Flags
        public bool? IsClearance { get; set; } = false;
        public bool? IsExclusive { get; set; } = false;
        public bool? IsNew { get; set; } = false;
        public bool? IsLowestPrice { get; set; } = false;
        public int? IsLimitQuantity { get; set; } = 0;
        public bool? IsOnlineOnly { get; set; } = false;
        public bool? IsNotAvailableOnline { get; set; } = false;
        public bool? IsRaincheck { get; set; } = false;
        public bool? IsOrderYoursToday { get; set; } = false;
        public bool? IsNotAvailableAllStores { get; set; } = false;
        public bool? IsLimitedStoreStock { get; set; } = false;
        public bool? IsWhileStockLast { get; set; } = false;
        public bool? IsStoreStockOnly { get; set; } = false;
        public string? ImageReference { get; set; }
        public string? CopyReference { get; set; }
        public string? OtherDetails { get; set; }
        public string? Comments { get; set; }

        // Audit Tracking
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public bool? IsOfferEdited { get; set; } = false;
        public bool? IsOfferActive { get; set; } = true;
    }
}
