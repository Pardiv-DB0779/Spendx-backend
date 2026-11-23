using GPC.Boost.SpendX.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GPC.Boost.SpendX.Data
{
    public class SpendXDbContext : DbContext
    {
        public SpendXDbContext(DbContextOptions<SpendXDbContext> options) : base(options)
        {
        }

        public virtual DbSet<TEventOffer> TEventOffers { get; set; }
        public virtual DbSet<TEventOfferDetail> TEventOfferDetails { get; set; }
        public virtual DbSet<TProduct> TProducts { get; set; }
        public virtual DbSet<TMudMap> TMudMaps { get; set; }
        public virtual DbSet<TEventOfferSearchHistory> TEventOfferSearchHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ------------------------------------------------------------------
            // 1. TProduct -> tproducts
            // ------------------------------------------------------------------
            modelBuilder.Entity<TProduct>(entity =>
            {
                entity.ToTable("tproducts");
                entity.HasKey(e => e.Sku).HasName("pk_tproducts");

                entity.Property(e => e.Sku).HasMaxLength(50).HasColumnName("sku");
                entity.Property(e => e.PartNo).HasMaxLength(50).HasColumnName("partNo");
                entity.Property(e => e.Description).HasMaxLength(255).HasColumnName("description");
                entity.Property(e => e.Brand).HasMaxLength(100).HasColumnName("brand");
                entity.Property(e => e.SupplierId).HasMaxLength(50).HasColumnName("supplierId");
                entity.Property(e => e.SupplierName).HasMaxLength(150).HasColumnName("supplierName");
                entity.Property(e => e.ItemClass1).HasMaxLength(50).HasColumnName("itemClass1");
                entity.Property(e => e.ItemClass1Description).HasMaxLength(150).HasColumnName("itemClass1Description");
                entity.Property(e => e.ItemClass2).HasMaxLength(50).HasColumnName("itemClass2");
                entity.Property(e => e.ItemClass2Description).HasMaxLength(150).HasColumnName("itemClass2Description");
                entity.Property(e => e.ItemClass3).HasMaxLength(50).HasColumnName("itemClass3");
                entity.Property(e => e.ItemClass3Description).HasMaxLength(150).HasColumnName("itemClass3Description");
                entity.Property(e => e.ItemClass4).HasMaxLength(50).HasColumnName("itemClass4");
                entity.Property(e => e.ItemClass4Description).HasMaxLength(150).HasColumnName("itemClass4Description");
                entity.Property(e => e.Country).HasMaxLength(10).HasColumnName("country");
                entity.Property(e => e.Company).HasMaxLength(50).HasColumnName("company");
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.Clearance).HasMaxLength(5).HasColumnName("clearance");

                entity.Property(e => e.EverydayPrice).HasPrecision(19, 5).HasColumnName("everydayPrice");
                entity.Property(e => e.EverydayPriceGst).HasPrecision(19, 5).HasColumnName("everydayPriceGst");
                entity.Property(e => e.ClearancePrice).HasPrecision(19, 5).HasColumnName("clearancePrice");
                entity.Property(e => e.SpecialPrice184).HasPrecision(19, 5).HasColumnName("specialPrice184");
                entity.Property(e => e.PrimaryPrice).HasPrecision(19, 5).HasColumnName("primaryPrice");
                entity.Property(e => e.FallbackPrice).HasPrecision(19, 5).HasColumnName("fallbackPrice");

                entity.Property(e => e.NationalAverageCost).HasPrecision(19, 5).HasColumnName("nationalAverageCost");
                entity.Property(e => e.LeCost).HasPrecision(19, 5).HasColumnName("leCost");
                entity.Property(e => e.VendorCostPerEach).HasPrecision(19, 5).HasColumnName("vendorCostPerEach");

                entity.Property(e => e.AverageMonthlySales).HasPrecision(19, 5).HasColumnName("averageMonthlySales");
                entity.Property(e => e.StockOnHandStore).HasPrecision(19, 5).HasColumnName("stockOnHandStore");
                entity.Property(e => e.StockOnHandDc).HasPrecision(19, 5).HasColumnName("stockOnHandDc");

                entity.Property(e => e.G0).HasColumnName("G0");
                entity.Property(e => e.G1).HasColumnName("G1");
                entity.Property(e => e.G2).HasColumnName("G2");
                entity.Property(e => e.G3).HasColumnName("G3");
                entity.Property(e => e.G4).HasColumnName("G4");
                entity.Property(e => e.G5).HasColumnName("G5");

                entity.Property(e => e.PricePoint6).HasPrecision(19, 5).HasColumnName("pricePoint6");
                entity.Property(e => e.PricePoint6IncludingGst).HasPrecision(19, 5).HasColumnName("pricePoint6IncludingGst");

                entity.HasIndex(e => new { e.Brand, e.Country, e.IsActive }, "idx_tproducts_brand_country");
            });

            // ------------------------------------------------------------------
            // 3. TEventOffer -> tEventOffer
            // ------------------------------------------------------------------
            modelBuilder.Entity<TEventOffer>(entity =>
            {
                entity.ToTable("tEventOffer");
                entity.HasKey(e => new { e.OfferId, e.OfferNumber }).HasName("pk_teventoffer");

                entity.Property(e => e.OfferId).ValueGeneratedOnAdd().HasColumnName("offerId");
                entity.Property(e => e.OfferNumber).HasColumnName("offerNumber");
                entity.Property(e => e.EventId).HasColumnName("eventId");
                entity.Property(e => e.Page).HasColumnName("page");
                entity.Property(e => e.PagePosition).HasColumnName("pagePosition");
                entity.Property(e => e.Status).HasMaxLength(50).HasColumnName("status");

                entity.Property(e => e.OfferTypeId).HasColumnName("offerTypeId");
                entity.Property(e => e.OfferType).HasMaxLength(100).HasColumnName("offerType");
                entity.Property(e => e.CommercialOfferType).HasMaxLength(100).HasColumnName("commercialOfferType");
                entity.Property(e => e.CommercialOfferItemClass1).HasMaxLength(100).HasColumnName("commercialOfferItemClass1");
                entity.Property(e => e.CommercialCategoryManager).HasMaxLength(100).HasColumnName("commercialCategoryManager");
                entity.Property(e => e.OfferName).HasMaxLength(255).HasColumnName("offerName");
                entity.Property(e => e.OfferDescription).HasMaxLength(500).HasColumnName("offerDescription");
                entity.Property(e => e.Brand).HasMaxLength(100).HasColumnName("brand");
                entity.Property(e => e.Country).HasMaxLength(10).HasColumnName("country");
                entity.Property(e => e.CategoryCostType).HasMaxLength(50).HasColumnName("categoryCostType");
                entity.Property(e => e.IsRewards).HasColumnName("isRewards");

                // SpendX 3-Tier Thresholds
                entity.Property(e => e.Spend1).HasPrecision(19, 5).HasColumnName("spend1");
                entity.Property(e => e.SavePct1).HasPrecision(19, 5).HasColumnName("savePct1");
                entity.Property(e => e.Spend2).HasPrecision(19, 5).HasColumnName("spend2");
                entity.Property(e => e.SavePct2).HasPrecision(19, 5).HasColumnName("savePct2");
                entity.Property(e => e.Spend3).HasPrecision(19, 5).HasColumnName("spend3");
                entity.Property(e => e.SavePct3).HasPrecision(19, 5).HasColumnName("savePct3");

                entity.Property(e => e.ThresholdAmount).HasPrecision(19, 5).HasColumnName("thresholdAmount");
                entity.Property(e => e.DiscountPercent).HasPrecision(19, 5).HasColumnName("discountPercent");
                entity.Property(e => e.DiscountAmount).HasPrecision(19, 5).HasColumnName("discountAmount");

                entity.Property(e => e.IncrementalPercentage).HasPrecision(19, 5).HasColumnName("incrementalPercentage");
                entity.Property(e => e.SpacePurchase).HasPrecision(19, 5).HasColumnName("spacePurchase");

                // Forecast & Financial Rollups
                entity.Property(e => e.ForecastUnits).HasColumnName("forecastUnits");
                entity.Property(e => e.EverydayUnits).HasPrecision(19, 5).HasColumnName("everydayUnits");
                entity.Property(e => e.IncrementalUnits).HasColumnName("incrementalUnits");
                entity.Property(e => e.ForecastCost).HasPrecision(19, 5).HasColumnName("forecastCost");
                entity.Property(e => e.ForecastSales).HasPrecision(19, 5).HasColumnName("forecastSales");
                entity.Property(e => e.ForecastTradeMargin).HasPrecision(19, 5).HasColumnName("forecastTradeMargin");
                entity.Property(e => e.ForecastTradeMarginDollar).HasPrecision(19, 5).HasColumnName("forecastTradeMarginDollar");
                entity.Property(e => e.ForecastTmPercent).HasPrecision(19, 5).HasColumnName("forecastTmPercent");
                entity.Property(e => e.ForecastTradeMarginPercent).HasPrecision(19, 5).HasColumnName("forecastTradeMarginPercent");
                entity.Property(e => e.IncrementalSales).HasPrecision(19, 5).HasColumnName("incrementalSales");
                entity.Property(e => e.IncrementalSalesDollar).HasPrecision(19, 5).HasColumnName("incrementalSalesDollar");
                entity.Property(e => e.IncrementalTm).HasPrecision(19, 5).HasColumnName("incrementalTm");
                entity.Property(e => e.IncrementalTmDollar).HasPrecision(19, 5).HasColumnName("incrementalTmDollar");
                entity.Property(e => e.TotalSupplierIncome).HasPrecision(19, 5).HasColumnName("totalSupplierIncome");
                entity.Property(e => e.TotalScanSupportDollar).HasPrecision(19, 5).HasColumnName("totalScanSupportDollar");
                entity.Property(e => e.TotalScanSupportPercent).HasPrecision(19, 5).HasColumnName("totalScanSupportPercent");

                // Summary Pricing
                entity.Property(e => e.EverydayPrice).HasPrecision(19, 5).HasColumnName("everydayPrice");
                entity.Property(e => e.EverydayPriceGst).HasPrecision(19, 5).HasColumnName("everydayPriceGst");
                entity.Property(e => e.AdvertisedPrice).HasPrecision(19, 5).HasColumnName("advertisedPrice");
                entity.Property(e => e.AdvertisedPriceGst).HasPrecision(19, 5).HasColumnName("advertisedPriceGst");
                entity.Property(e => e.SaveValue).HasPrecision(19, 5).HasColumnName("saveValue");
                entity.Property(e => e.SavePercent).HasPrecision(19, 5).HasColumnName("savePercent");
                entity.Property(e => e.CalculatedSavePercent).HasPrecision(19, 5).HasColumnName("calculatedSavePercent");
                entity.Property(e => e.CalculatedSaveValue).HasPrecision(19, 5).HasColumnName("calculatedSaveValue");

                // Badges & Flags
                entity.Property(e => e.IsClearance).HasColumnName("isClearance");
                entity.Property(e => e.IsExclusive).HasColumnName("isExclusive");
                entity.Property(e => e.IsNew).HasColumnName("isNew");
                entity.Property(e => e.IsLowestPrice).HasColumnName("isLowestPrice");
                entity.Property(e => e.IsLimitQuantity).HasColumnName("isLimitQuantity");
                entity.Property(e => e.IsOnlineOnly).HasColumnName("isOnlineOnly");
                entity.Property(e => e.IsNotAvailableOnline).HasColumnName("isNotAvailableOnline");
                entity.Property(e => e.IsRaincheck).HasColumnName("isRaincheck");
                entity.Property(e => e.IsOrderYoursToday).HasColumnName("isOrderYoursToday");
                entity.Property(e => e.IsNotAvailableAllStores).HasColumnName("IsNotAvailableAllStores");
                entity.Property(e => e.IsLimitedStoreStock).HasColumnName("IsLimitedStoreStock");
                entity.Property(e => e.IsWhileStockLast).HasColumnName("isWhileStockLast");
                entity.Property(e => e.IsStoreStockOnly).HasColumnName("IsStoreStockOnly");
                entity.Property(e => e.ImageReference).HasMaxLength(255).HasColumnName("imageReference");
                entity.Property(e => e.CopyReference).HasMaxLength(255).HasColumnName("copyReference");
                entity.Property(e => e.OtherDetails).HasColumnName("OtherDetails");
                entity.Property(e => e.Comments).HasColumnName("Comments");

                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
                entity.Property(e => e.CreatedBy).HasMaxLength(100).HasColumnName("createdBy");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
                entity.Property(e => e.UpdatedBy).HasMaxLength(100).HasColumnName("updatedBy");
                entity.Property(e => e.IsOfferEdited).HasColumnName("isOfferEdited");
                entity.Property(e => e.IsOfferActive).HasColumnName("isOfferActive");

                entity.HasIndex(e => new { e.Page, e.PagePosition }, "idx_teventoffer_page_pos");
                entity.HasIndex(e => new { e.Brand, e.Country }, "idx_teventoffer_brand_country");
            });

            // ------------------------------------------------------------------
            // 4. TEventOfferDetail -> teventofferdetail
            // ------------------------------------------------------------------
            modelBuilder.Entity<TEventOfferDetail>(entity =>
            {
                entity.ToTable("teventofferdetail");
                entity.HasKey(e => new { e.OfferId, e.OfferNo, e.Sku }).HasName("pk_teventofferdetail");

                entity.Property(e => e.OfferId).HasColumnName("offerId");
                entity.Property(e => e.OfferNo).HasColumnName("offerNo");
                entity.Property(e => e.Sku).HasMaxLength(50).HasColumnName("sku");
                entity.Property(e => e.PartNo).HasMaxLength(50).HasColumnName("partNo");
                entity.Property(e => e.Description).HasMaxLength(255).HasColumnName("description");
                entity.Property(e => e.Brand).HasMaxLength(100).HasColumnName("brand");
                entity.Property(e => e.ItemClass1).HasMaxLength(50).HasColumnName("itemClass1");
                entity.Property(e => e.ItemClass2).HasMaxLength(50).HasColumnName("itemClass2");
                entity.Property(e => e.ItemClass3).HasMaxLength(50).HasColumnName("itemClass3");
                entity.Property(e => e.ItemClass4).HasMaxLength(50).HasColumnName("itemClass4");
                entity.Property(e => e.UnitOfMeasureCopy).HasMaxLength(50).HasColumnName("unitOfMeasureCopy");
                entity.Property(e => e.SubstituteSku).HasMaxLength(50).HasColumnName("substituteSku");

                entity.Property(e => e.EverydayPrice).HasPrecision(19, 5).HasColumnName("everydayPrice");
                entity.Property(e => e.EverydayPriceGst).HasPrecision(19, 5).HasColumnName("everydayPriceGst");
                entity.Property(e => e.EverydayPriceGstSys).HasPrecision(19, 5).HasColumnName("everydayPriceGstSys");
                entity.Property(e => e.AdvertisedPrice).HasPrecision(19, 5).HasColumnName("advertisedPrice");
                entity.Property(e => e.AdvertisedPriceGst).HasPrecision(19, 5).HasColumnName("advertisedPriceGst");
                entity.Property(e => e.CalculatedSavePercentage).HasPrecision(19, 5).HasColumnName("calculatedSavePercentage");
                entity.Property(e => e.CalculatedSaveValue).HasPrecision(19, 5).HasColumnName("calculatedSaveValue");
                entity.Property(e => e.FromPriceIndicator).HasColumnName("fromPriceIndicator");
                entity.Property(e => e.IsFromPriceOverridden).HasColumnName("isFromPriceOverridden");

                entity.Property(e => e.EverydayUnits).HasPrecision(19, 5).HasColumnName("everydayUnits");
                entity.Property(e => e.ForecastUnits).HasPrecision(19, 5).HasColumnName("forecastUnits");
                entity.Property(e => e.Categoryforecast).HasColumnName("categoryforecast");
                entity.Property(e => e.IncrementalForecast).HasColumnName("incrementalForecast");
                entity.Property(e => e.ForecastCost).HasPrecision(19, 5).HasColumnName("forecastCost");
                entity.Property(e => e.ForecastSales).HasPrecision(19, 5).HasColumnName("forecastSales");
                entity.Property(e => e.ForecastTradeMarginDollar).HasPrecision(19, 5).HasColumnName("forecastTradeMarginDollar");
                entity.Property(e => e.ForecastTradeMarginPercent).HasPrecision(19, 5).HasColumnName("forecastTradeMarginPercent");
                entity.Property(e => e.EverydayTradeMarginDollar).HasPrecision(19, 5).HasColumnName("everydayTradeMarginDollar");
                entity.Property(e => e.EverydayTradeMarginPercent).HasPrecision(19, 5).HasColumnName("everydayTradeMarginPercent");
                entity.Property(e => e.IncrementalSales).HasPrecision(19, 5).HasColumnName("incrementalSales");
                entity.Property(e => e.IncrementalTradeDollar).HasPrecision(19, 5).HasColumnName("incrementalTradeDollar");

                entity.Property(e => e.ScanSupportDollar).HasPrecision(19, 5).HasColumnName("scanSupport$");
                entity.Property(e => e.ScanSupportValue).HasPrecision(19, 5).HasColumnName("scanSupportValue");
                entity.Property(e => e.ScanSupportPercent).HasPrecision(19, 5).HasColumnName("scanSupportPercent");
                entity.Property(e => e.LeCost).HasPrecision(19, 5).HasColumnName("leCost");
                entity.Property(e => e.LatestEffectiveCost).HasPrecision(19, 5).HasColumnName("LatestEffectiveCost");
                entity.Property(e => e.NationalAverageCost).HasPrecision(19, 5).HasColumnName("nationalAverageCost");
                entity.Property(e => e.CategoryCost).HasPrecision(19, 5).HasColumnName("categoryCost");
                entity.Property(e => e.VendorCostPerEach).HasPrecision(19, 5).HasColumnName("vendorCostPerEach");
                entity.Property(e => e.EverydayCost).HasPrecision(19, 5).HasColumnName("everydayCost");
                entity.Property(e => e.EverydayExtendedUnitCost).HasPrecision(19, 5).HasColumnName("everydayExtendedUnitCost");
                entity.Property(e => e.EverydayExtendedUnitSales).HasPrecision(19, 5).HasColumnName("everydayExtendedUnitSales");
                entity.Property(e => e.ExtendedAdvertisedPrice).HasPrecision(19, 5).HasColumnName("extendedAdvertisedPrice");

                entity.Property(e => e.FutureEdPrice).HasPrecision(19, 5).HasColumnName("futureEdPrice");
                entity.Property(e => e.FutureEdEffectiveDate).HasColumnName("futureEdEffectiveDate");

                entity.Property(e => e.ClearanceIndicator).HasMaxLength(5).HasColumnName("clearanceIndicator");
                entity.Property(e => e.ShowroomIndicator).HasMaxLength(5).HasColumnName("showroomIndicator");
                entity.Property(e => e.StockOnHandStore).HasPrecision(19, 5).HasColumnName("stockOnHandStore");
                entity.Property(e => e.StockOnHandDc).HasPrecision(19, 5).HasColumnName("stockOnHandDc");
                entity.Property(e => e.IsSkuActive).HasColumnName("isSkuActive");
                entity.Property(e => e.IsSkuEdited).HasColumnName("isSkuEdited");
                entity.Property(e => e.DisplayIndicator).HasColumnName("displayIndicator");

                entity.Property(e => e.G0).HasColumnName("G0");
                entity.Property(e => e.G1).HasColumnName("G1");
                entity.Property(e => e.G2).HasColumnName("G2");
                entity.Property(e => e.G3).HasColumnName("G3");
                entity.Property(e => e.G4).HasColumnName("G4");
                entity.Property(e => e.G5).HasColumnName("G5");
                entity.Property(e => e.Group0Quantity).HasColumnName("group0Quantity");
                entity.Property(e => e.Group1Quantity).HasColumnName("group1Quantity");
                entity.Property(e => e.Group2Quantity).HasColumnName("group2Quantity");
                entity.Property(e => e.Group3Quantity).HasColumnName("group3Quantity");
                entity.Property(e => e.Group4Quantity).HasColumnName("group4Quantity");
                entity.Property(e => e.Group5Quantity).HasColumnName("group5Quantity");
                entity.Property(e => e.TotalTieUp).HasColumnName("totalTieUp");
                entity.Property(e => e.TieUpCost).HasPrecision(19, 5).HasColumnName("tieUpCost");

                entity.Property(e => e.PricePoint6).HasPrecision(19, 5).HasColumnName("pricePoint6");
                entity.Property(e => e.PricePoint6IncludingGst).HasPrecision(19, 5).HasColumnName("pricePoint6IncludingGst");

                entity.Property(e => e.Page).HasColumnName("page");
                entity.Property(e => e.PagePosition).HasColumnName("pagePosition");

                entity.HasIndex(e => e.Sku, "idx_teventofferdetail_sku");
                entity.HasIndex(e => new { e.OfferId, e.OfferNo }, "idx_teventofferdetail_offer");
            });

            // ------------------------------------------------------------------
            // 5. TMudMap -> tmudmap
            // ------------------------------------------------------------------
            modelBuilder.Entity<TMudMap>(entity =>
            {
                entity.ToTable("tmudmap");
                entity.HasKey(e => e.MudMapId).HasName("pk_tmudmap");

                entity.Property(e => e.MudMapId).ValueGeneratedOnAdd().HasColumnName("mudMapId");
                entity.Property(e => e.EventId).HasColumnName("eventId");
                entity.Property(e => e.Country).HasMaxLength(10).HasColumnName("country");
                entity.Property(e => e.PageId).HasColumnName("pageId");
                entity.Property(e => e.PositionId).HasColumnName("positionId");
                entity.Property(e => e.OfferId).HasColumnName("offerId");
                entity.Property(e => e.OfferNumber).HasColumnName("offerNumber");
                entity.Property(e => e.OfferName).HasMaxLength(255).HasColumnName("offerName");
                entity.Property(e => e.OfferType).HasMaxLength(100).HasColumnName("offerType");
                entity.Property(e => e.Brand).HasMaxLength(100).HasColumnName("brand");

                entity.Property(e => e.EverydayPrice).HasPrecision(19, 5).HasColumnName("everydayPrice");
                entity.Property(e => e.AdvertisedPrice).HasPrecision(19, 5).HasColumnName("advertisedPrice");
                entity.Property(e => e.SaveValue).HasPrecision(19, 5).HasColumnName("saveValue");
                entity.Property(e => e.SavePercent).HasPrecision(19, 5).HasColumnName("savePercent");
                entity.Property(e => e.IsReserved).HasColumnName("isReserved");
                entity.Property(e => e.Clearance).HasColumnName("clearance");
                entity.Property(e => e.New).HasColumnName("new");
                entity.Property(e => e.Loyalty).HasColumnName("loyalty");
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.UserName).HasMaxLength(100).HasColumnName("userName");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

                entity.HasIndex(e => e.OfferId, "idx_tmudmap_offerId");
                entity.HasIndex(e => new { e.EventId, e.PageId, e.PositionId }, "idx_tmudmap_event_page_pos");
            });

            // ------------------------------------------------------------------
            // 6. TEventOfferSearchHistory -> tEventOfferSearchHistory
            // ------------------------------------------------------------------
            modelBuilder.Entity<TEventOfferSearchHistory>(entity =>
            {
                entity.ToTable("tEventOfferSearchHistory");
                entity.HasKey(e => e.Id).HasName("tEventOfferSearchHistory_pkey");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EventId).HasColumnName("eventId");
                entity.Property(e => e.PageId).HasColumnName("pageId");
                entity.Property(e => e.PositionId).HasColumnName("positionId");
                entity.Property(e => e.EventOfferId).HasColumnName("eventOfferId");
                entity.Property(e => e.OfferNumber).HasColumnName("offerNumber");
                entity.Property(e => e.SearchJson).HasColumnType("jsonb").HasColumnName("searchJson");
                entity.Property(e => e.UserId).HasMaxLength(100).HasColumnName("userId");
                entity.Property(e => e.Country).HasMaxLength(10).HasColumnName("country");
                entity.Property(e => e.SearchedAt).HasColumnType("timestamp without time zone").HasColumnName("searchedAt");

                entity.HasIndex(e => new { e.EventId, e.PageId, e.PositionId }, "idx_teventoffersearchhistory");
            });
        }
    }
}
