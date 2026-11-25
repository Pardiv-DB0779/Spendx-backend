using System;
using System.Collections.Generic;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class SpendXTierDto
    {
        public int TierNumber { get; set; }
        public decimal SpendThreshold { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class SpendXOverviewDto
    {
        public string? OfferType { get; set; }
        public string? OfferName { get; set; }
        public string? Brand { get; set; }
        public bool Loyalty { get; set; }
        public bool? IsTiered { get; set; }
        public string? RepresentativePartNumber { get; set; }

        public decimal Spend1 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }

        public int ForecastUnits { get; set; }
        public int IncrementalUnits { get; set; }
        public decimal ForecastCost { get; set; }
        public decimal ForecastSales { get; set; }
        public decimal ForecastTmDollar { get; set; }
        public decimal ForecastTmPercent { get; set; }
        public decimal TotalSupplierIncome { get; set; }
    }

    public class SpendXOfferInfoDto
    {
        public bool Clearance { get; set; }
        public bool Exclusive { get; set; }
        public bool New { get; set; }
        public bool LowestPrice { get; set; }
        public bool LimitedQuantity { get; set; }
        public bool OnlineOnly { get; set; }
        public bool NotAvailOnline { get; set; }
        public bool NoRainCheck { get; set; }
        public bool OrderYoursToday { get; set; }
        public bool NotAvailableAllStores { get; set; }
        public bool LimitedStoreStock { get; set; }
        public bool WhileStocksLast { get; set; }
        public bool WhileStockLast { get => WhileStocksLast; set => WhileStocksLast = value; }
        public bool StoreStockOnly { get; set; }
        public string? ImageReference { get; set; }
        public string? CopyReference { get; set; }
        public string? Other { get; set; }
    }

    public class SpendXForecastingDto
    {
        public int ForecastUnits { get; set; }
        public int IncrementalUnits { get; set; }
        public decimal EverydayUnits { get; set; }
        public decimal StoreSOH { get; set; }
        public decimal DcSOH { get; set; }
    }

    public class SpendXPricingDto
    {
        public bool? IsTiered { get; set; }
        public decimal Spend1 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }

        public decimal? SpacePurchase { get; set; }
        public decimal ForecastCost { get; set; }
        public decimal ForecastSales { get; set; }
        public decimal ForecastTmDollar { get; set; }
        public decimal ForecastTmPercent { get; set; }
    }

    public class SpendXOfferDto : APIResponseDto
    {
        public int OfferId { get; set; }
        public int OfferNo { get; set; }
        public int PageId { get; set; }
        public int PositionId { get; set; }
        public string Country { get; set; } = "AU";
        public decimal GST { get; set; } = 0.10m;

        public SpendXOverviewDto Overview { get; set; } = new();
        public SpendXOfferInfoDto OfferInfo { get; set; } = new();
        public SpendXForecastingDto Forecasting { get; set; } = new();
        public SpendXPricingDto Pricing { get; set; } = new();
        public List<SpendXTierDto> Tiers { get; set; } = new();
    }
}
