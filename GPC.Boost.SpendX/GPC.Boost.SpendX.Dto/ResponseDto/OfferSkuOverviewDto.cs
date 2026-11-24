using System;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class OfferSkuOverviewDto
    {
        public string Sku { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool FromPriceInd { get; set; }
        public bool DisplayInd { get; set; } = true;
        public string? Clearance { get; set; }
        public string? Showroom { get; set; }

        // Pricing
        public decimal AdvPriceGst { get; set; }
        public decimal AdvPriceExGst { get; set; }
        public decimal EdPrice { get; set; }
        public decimal EdPriceGst { get; set; }
        public decimal CalcSavePercent { get; set; }
        public decimal CalcSaveValue { get; set; }
        public decimal LeCost { get; set; }
        public decimal NatAvgCost { get; set; }

        // Forecast & Volume
        public int CatFcst { get; set; }
        public int IncrFcst { get; set; }
        public decimal EdUnits { get; set; }
        public decimal FcstCost { get; set; }
        public decimal FcstSales { get; set; }
        public decimal FcstTmValue { get; set; }
        public decimal FcstTmPercent { get; set; }

        // SOH & Inventory
        public int SohStore { get; set; }
        public int SohDc { get; set; }
        public int Grp0Qty { get; set; }
        public int Grp1Qty { get; set; }
        public int Grp2Qty { get; set; }
        public int Grp3Qty { get; set; }
        public int Grp4Qty { get; set; }
        public int Grp5Qty { get; set; }
        public int TotalTieUp { get; set; }
        public decimal TieUpPercentFcst { get; set; }
        public decimal TieUpCost { get; set; }

        // Supplier funding & Hierarchy
        public decimal ScanSupportValue { get; set; }
        public decimal ScanSupportPercent { get; set; }
        public string? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? IC2 { get; set; }
        public string? IC3 { get; set; }
        public string? IC4 { get; set; }
        public string? ComOfferIC1 { get; set; }

        // Financial Extensions
        public decimal? IncrementalTmDollar { get; set; }
        public decimal? IncrementalSalesDollar { get; set; }
        public decimal? EdCost { get; set; }
        public decimal? EDUnitTmDollar { get; set; }
        public decimal? AdvUnitTmDollar { get; set; }
        public decimal? AdvSalesDollar { get; set; }
        public decimal? EdSalesDollar { get; set; }

        // SpendX Tiers
        public decimal Spend1 { get; set; }
        public decimal SavePct1 { get; set; }
        public decimal? Spend2 { get; set; }
        public decimal? SavePct2 { get; set; }
        public decimal? Spend3 { get; set; }
        public decimal? SavePct3 { get; set; }
        public bool IsTiered { get; set; }

        // Future Price
        public decimal? FutureEdPrice { get; set; }
        public DateOnly? FutureEdEffectiveDate { get; set; }

        // Flags
        public bool? isCatFcstLocked { get; set; }
        public bool? isSkuEdited { get; set; }
        public bool? isSkuActive { get; set; } = true;
        public bool? isFromPriceOverriden { get; set; }
    }
}
