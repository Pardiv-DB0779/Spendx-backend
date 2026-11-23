using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TEventOfferDetail
    {
        public int OfferId { get; set; }
        public int OfferNo { get; set; } = 1;
        public string Sku { get; set; } = null!;
        public string? PartNo { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? ItemClass1 { get; set; }
        public string? ItemClass2 { get; set; }
        public string? ItemClass3 { get; set; }
        public string? ItemClass4 { get; set; }
        public string? UnitOfMeasureCopy { get; set; }
        public string? SubstituteSku { get; set; }

        // Pricing & Save Values
        public decimal? EverydayPrice { get; set; }
        public decimal? EverydayPriceGst { get; set; }
        public decimal? EverydayPriceGstSys { get; set; }
        public decimal? AdvertisedPrice { get; set; }
        public decimal? AdvertisedPriceGst { get; set; }
        public decimal? CalculatedSavePercentage { get; set; }
        public decimal? CalculatedSaveValue { get; set; }
        public bool? FromPriceIndicator { get; set; } = false;
        public bool? IsFromPriceOverridden { get; set; } = false;

        // Forecasts & Financials
        public decimal? EverydayUnits { get; set; }
        public decimal? ForecastUnits { get; set; }
        public int? Categoryforecast { get; set; }
        public int? IncrementalForecast { get; set; }
        public decimal? ForecastCost { get; set; }
        public decimal? ForecastSales { get; set; }
        public decimal? ForecastTradeMarginDollar { get; set; }
        public decimal? ForecastTradeMarginPercent { get; set; }
        public decimal? EverydayTradeMarginDollar { get; set; }
        public decimal? EverydayTradeMarginPercent { get; set; }
        public decimal? IncrementalSales { get; set; }
        public decimal? IncrementalTradeDollar { get; set; }

        // Vendor Scan Support & Costs
        public decimal? ScanSupportDollar { get; set; }
        public decimal? ScanSupportValue { get; set; }
        public decimal? ScanSupportPercent { get; set; }
        public decimal? LeCost { get; set; }
        public decimal? LatestEffectiveCost { get; set; }
        public decimal? NationalAverageCost { get; set; }
        public decimal? CategoryCost { get; set; }
        public decimal? VendorCostPerEach { get; set; }
        public decimal? EverydayCost { get; set; }
        public decimal? EverydayExtendedUnitCost { get; set; }
        public decimal? EverydayExtendedUnitSales { get; set; }
        public decimal? ExtendedAdvertisedPrice { get; set; }

        // Future Price Tracking
        public decimal? FutureEdPrice { get; set; }
        public DateTime? FutureEdEffectiveDate { get; set; }

        // Operational & Inventory Flags
        public string? ClearanceIndicator { get; set; }
        public string? ShowroomIndicator { get; set; }
        public decimal? StockOnHandStore { get; set; }
        public decimal? StockOnHandDc { get; set; }
        public bool? IsSkuActive { get; set; } = true;
        public bool? IsSkuEdited { get; set; } = false;
        public bool? DisplayIndicator { get; set; } = true;
        public bool? IsCategoryForecastLocked { get; set; } = false;

        // Store Group Allocations & TieUp
        public int? G0 { get; set; }
        public int? G1 { get; set; }
        public int? G2 { get; set; }
        public int? G3 { get; set; }
        public int? G4 { get; set; }
        public int? G5 { get; set; }
        public int? Group0Quantity { get; set; } = 0;
        public int? Group1Quantity { get; set; } = 0;
        public int? Group2Quantity { get; set; } = 0;
        public int? Group3Quantity { get; set; } = 0;
        public int? Group4Quantity { get; set; } = 0;
        public int? Group5Quantity { get; set; } = 0;
        public int? TotalTieUp { get; set; } = 0;
        public decimal? TieUpCost { get; set; } = 0;

        public decimal? PricePoint6 { get; set; }
        public decimal? PricePoint6IncludingGst { get; set; }

        // MudMap Location
        public int Page { get; set; } = 1;
        public int PagePosition { get; set; } = 1;
    }
}
