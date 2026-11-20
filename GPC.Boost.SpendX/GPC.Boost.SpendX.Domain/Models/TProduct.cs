using System;

namespace GPC.Boost.SpendX.Domain.Models
{
    public class TProduct
    {
        // Core Identifiers & Brand (CRITICAL for SpendX brand-based selection)
        public string Sku { get; set; } = null!;
        public string PartNo { get; set; } = null!;
        public string? Description { get; set; }
        public string Brand { get; set; } = null!;
        public string? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        // Taxonomy
        public string? ItemClass1 { get; set; }
        public string? ItemClass1Description { get; set; }
        public string? ItemClass2 { get; set; }
        public string? ItemClass2Description { get; set; }
        public string? ItemClass3 { get; set; }
        public string? ItemClass3Description { get; set; }
        public string? ItemClass4 { get; set; }
        public string? ItemClass4Description { get; set; }

        public string Country { get; set; } = "AU";
        public string Company { get; set; } = "REPCO";
        public bool? IsActive { get; set; } = true;
        public string? Clearance { get; set; }

        // Pricing Waterfall & Baseline Inputs (replaces tPriceListDetail / tPriceList dependencies)
        public decimal? EverydayPrice { get; set; }
        public decimal? EverydayPriceGst { get; set; }
        public decimal? ClearancePrice { get; set; }
        public decimal? SpecialPrice184 { get; set; }
        public decimal? PrimaryPrice { get; set; }
        public decimal? FallbackPrice { get; set; }

        // Costing Inputs (replaces tProdSupCost / tNatAvgCost dependencies)
        public decimal? NationalAverageCost { get; set; }
        public decimal? LeCost { get; set; }
        public decimal? VendorCostPerEach { get; set; }

        // Sales & Units Inputs (replaces tSalesY1 dependencies)
        public decimal? AverageMonthlySales { get; set; } // Baseline EDUnits = (AverageMonthlySales / 30) * EventDays

        // Inventory Stock On Hand (replaces tInventory dependencies)
        public decimal? StockOnHandStore { get; set; }
        public decimal? StockOnHandDc { get; set; }

        // Regional Area Group Allocation (replaces tRegionalAreaGroupAllocation dependencies)
        public int? G0 { get; set; }
        public int? G1 { get; set; }
        public int? G2 { get; set; }
        public int? G3 { get; set; }
        public int? G4 { get; set; }
        public int? G5 { get; set; }

        // Price Point Rules (replaces tPriceProductRules dependencies)
        public decimal? PricePoint6 { get; set; }
        public decimal? PricePoint6IncludingGst { get; set; }
    }
}
