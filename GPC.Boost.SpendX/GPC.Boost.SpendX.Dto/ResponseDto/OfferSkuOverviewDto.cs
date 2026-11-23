using System;

namespace GPC.Boost.SpendX.Dto.ResponseDto
{
    public class OfferSkuOverviewDto
    {
        public string Sku { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Pricing (SpendX has everyday price; discounts applied at basket tier)
        public decimal EdPrice { get; set; }
        public decimal EdPriceGst { get; set; }
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
        public int TotalTieUp { get; set; }
        public decimal TieUpCost { get; set; }

        // Supplier funding
        public decimal ScanSupportValue { get; set; }
        public decimal ScanSupportPercent { get; set; }

        // Merchandising flags (No FromPrice)
        public bool DisplayInd { get; set; } = true;
        public string? Clearance { get; set; }
        public string? Showroom { get; set; }

        public bool? isCatFcstLocked { get; set; }
        public bool? isSkuEdited { get; set; }
        public bool? isSkuActive { get; set; } = true;
    }
}
