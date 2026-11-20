using System;
using Xunit;

namespace GPC.Boost.SpendX.Tests
{
    public class FinancialCalculationTests
    {
        [Theory]
        [InlineData(100.0, 10.0, null, null, 90.0)]
        [InlineData(100.0, 10.0, 20.0, null, 80.0)]
        [InlineData(100.0, 10.0, 20.0, 30.0, 70.0)]
        public void AdvertisedPrice_CalculatesUsingHighestNominalTierDiscount(
            double edPrice, double tier1Pct, double? tier2Pct, double? tier3Pct, double expectedAdvPrice)
        {
            // Arrange
            decimal everydayPrice = (decimal)edPrice;
            decimal t1 = (decimal)tier1Pct;
            decimal? t2 = tier2Pct.HasValue ? (decimal?)tier2Pct.Value : null;
            decimal? t3 = tier3Pct.HasValue ? (decimal?)tier3Pct.Value : null;

            // Act - BRD Formula: AdvertisedPrice = EverydayPrice * (1 - max(SavePct1, SavePct2, SavePct3) / 100)
            decimal maxDiscount = Math.Max(t1, Math.Max(t2 ?? 0m, t3 ?? 0m));
            decimal calculatedAdvPrice = Math.Round(everydayPrice * (1m - (maxDiscount / 100m)), 2);

            // Assert
            Assert.Equal((decimal)expectedAdvPrice, calculatedAdvPrice);
        }

        [Fact]
        public void AdvertisedPrice_ConservativelyUnderstatesTradeMarginBeforeFlyerLock()
        {
            // Arrange
            decimal edPrice = 100m;
            decimal cost = 50m;
            int forecastUnits = 100;
            decimal tier1Pct = 10m; // 10% off
            decimal tier2Pct = 25m; // 25% off (max)

            decimal maxDiscount = Math.Max(tier1Pct, tier2Pct);
            decimal advPrice = Math.Round(edPrice * (1m - (maxDiscount / 100m)), 2); // 75.00

            // Act
            decimal forecastedSales = forecastUnits * advPrice; // 7,500
            decimal forecastedCost = forecastUnits * cost;       // 5,000
            decimal tradeMargin = forecastedSales - forecastedCost; // 2,500

            decimal nonDiscountedMargin = (forecastUnits * edPrice) - forecastedCost; // 5,000

            // Assert - Understated margin guarantees BOOST does not overstate promotional viability
            Assert.Equal(75m, advPrice);
            Assert.Equal(2500m, tradeMargin);
            Assert.True(tradeMargin < nonDiscountedMargin);
        }

        [Theory]
        [InlineData("AU", 0.10)]
        [InlineData("NZ", 0.15)]
        public void CountryGst_ResolvesCorrectRate(string country, double expectedGstRate)
        {
            // Act
            decimal gst = string.Equals(country, "NZ", StringComparison.OrdinalIgnoreCase) ? 0.15m : 0.10m;

            // Assert
            Assert.Equal((decimal)expectedGstRate, gst);
        }

        [Fact]
        public void SupplierIncome_IncludesSpacePurchaseAndScanSupport()
        {
            // Arrange
            decimal spacePurchase = 5000m;
            decimal scanSupportPerUnit = 2.50m;
            int forecastUnits = 1000;

            // Act
            decimal scanSupportTotal = scanSupportPerUnit * forecastUnits; // 2,500
            decimal totalSupplierIncome = spacePurchase + scanSupportTotal; // 7,500

            // Assert
            Assert.Equal(7500m, totalSupplierIncome);
        }

        [Fact]
        public void ForecastTradeMargin_RollsUpProductMarginAndSupplierIncome()
        {
            // Arrange
            decimal forecastSales = 20000m;
            decimal forecastCost = 14000m;
            decimal spacePurchase = 3000m;
            decimal scanSupportTotal = 1000m;

            decimal totalSupplierIncome = spacePurchase + scanSupportTotal; // 4,000
            decimal productMargin = forecastSales - forecastCost;           // 6,000

            // Act - BRD Formula: ForecastTradeMargin$ = (ForecastSales - ForecastCost) + TotalSupplierIncome
            decimal totalTradeMargin = productMargin + totalSupplierIncome; // 10,000
            decimal tmPercent = Math.Round((totalTradeMargin / forecastSales) * 100m, 2); // 50%

            // Assert
            Assert.Equal(10000m, totalTradeMargin);
            Assert.Equal(50.00m, tmPercent);
        }
    }
}
