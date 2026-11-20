using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Domain.Models;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Repository;
using GPC.Boost.SpendX.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GPC.Boost.SpendX.Tests
{
    public class SpendXServiceTests
    {
        private readonly Mock<ISpendXRepository> _repoMock;
        private readonly Mock<ILogger<SpendXService>> _loggerMock;
        private readonly SpendXService _service;

        public SpendXServiceTests()
        {
            _repoMock = new Mock<ISpendXRepository>();
            _loggerMock = new Mock<ILogger<SpendXService>>();
            _service = new SpendXService(_repoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task AddEventOffer_LoyaltyOffer_AppendsLoyalSuffix()
        {
            // Arrange
            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU001", Brand = "VALVOLINE", Country = "AU", EverydayPrice = 100m, EverydayPriceGst = 110m, IsActive = true }
            };

            _repoMock.Setup(r => r.GetProductsByBrandsAsync(It.IsAny<List<string>>(), "AU", null))
                     .ReturnsAsync(products);

            TEventOffer? capturedOffer = null;
            _repoMock.Setup(r => r.SaveOrUpdateSpendXOfferAsync(It.IsAny<TEventOffer>(), It.IsAny<List<TEventOfferDetail>>(), false, false))
                     .Callback<TEventOffer, List<TEventOfferDetail>, bool, bool>((o, d, u, res) => capturedOffer = o)
                     .ReturnsAsync((true, 101));

            var request = new EventOfferRequestDto
            {
                OfferTypeId = 118, // SpendX Loyal
                IsRewards = true,
                ProductBrand = "VALVOLINE",
                OfferName = "Valvoline Special",
                Spend1 = 50m,
                SavePct1 = 10m
            };

            // Act
            var response = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(101, response.OfferId);
            Assert.NotNull(capturedOffer);
            Assert.Equal("Valvoline Special - Loyal", capturedOffer.OfferName);
            Assert.True(capturedOffer.IsRewards);
            Assert.Equal(118, capturedOffer.OfferTypeId);
        }

        [Fact]
        public async Task AddEventOffer_NonLoyalOffer_StripsLoyalSuffixIfPresent()
        {
            // Arrange
            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU001", Brand = "VALVOLINE", Country = "AU", EverydayPrice = 100m, EverydayPriceGst = 110m, IsActive = true }
            };

            _repoMock.Setup(r => r.GetProductsByBrandsAsync(It.IsAny<List<string>>(), "AU", null))
                     .ReturnsAsync(products);

            TEventOffer? capturedOffer = null;
            _repoMock.Setup(r => r.SaveOrUpdateSpendXOfferAsync(It.IsAny<TEventOffer>(), It.IsAny<List<TEventOfferDetail>>(), false, false))
                     .Callback<TEventOffer, List<TEventOfferDetail>, bool, bool>((o, d, u, res) => capturedOffer = o)
                     .ReturnsAsync((true, 102));

            var request = new EventOfferRequestDto
            {
                OfferTypeId = 18, // Standard SpendX
                IsRewards = false,
                ProductBrand = "VALVOLINE",
                OfferName = "Valvoline Special - Loyal", // Input has - Loyal, but offer is non-loyal
                Spend1 = 50m,
                SavePct1 = 10m
            };

            // Act
            var response = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(capturedOffer);
            Assert.Equal("Valvoline Special", capturedOffer.OfferName);
            Assert.False(capturedOffer.IsRewards);
            Assert.Equal(18, capturedOffer.OfferTypeId);
        }

        [Fact]
        public async Task AddEventOffer_ExcludesUnselectedSKUsDeterministically()
        {
            // Arrange
            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU001", Brand = "VALVOLINE", Country = "AU", EverydayPrice = 100m, EverydayPriceGst = 110m, IsActive = true },
                new TProduct { Sku = "SKU002", Brand = "VALVOLINE", Country = "AU", EverydayPrice = 120m, EverydayPriceGst = 132m, IsActive = true },
                new TProduct { Sku = "SKU003", Brand = "VALVOLINE", Country = "AU", EverydayPrice = 80m, EverydayPriceGst = 88m, IsActive = true }
            };

            _repoMock.Setup(r => r.GetProductsByBrandsAsync(It.IsAny<List<string>>(), "AU", null))
                     .ReturnsAsync(products);

            List<TEventOfferDetail>? capturedDetails = null;
            _repoMock.Setup(r => r.SaveOrUpdateSpendXOfferAsync(It.IsAny<TEventOffer>(), It.IsAny<List<TEventOfferDetail>>(), false, false))
                     .Callback<TEventOffer, List<TEventOfferDetail>, bool, bool>((o, d, u, res) => capturedDetails = d)
                     .ReturnsAsync((true, 103));

            var request = new EventOfferRequestDto
            {
                OfferTypeId = 18,
                ProductBrand = "VALVOLINE",
                Spend1 = 50m,
                SavePct1 = 10m,
                UnSelectedSKUs = new List<string> { "SKU002" } // Exclude SKU002
            };

            // Act
            var response = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(2, response.SkuCount);
            Assert.NotNull(capturedDetails);
            Assert.DoesNotContain(capturedDetails, d => d.Sku == "SKU002");
            Assert.Contains(capturedDetails, d => d.Sku == "SKU001");
            Assert.Contains(capturedDetails, d => d.Sku == "SKU003");
        }

        [Fact]
        public async Task SaveEventOfferOverview_ReturnsImmediateRecalculatedFinancials()
        {
            // Arrange
            var existingOffer = new TEventOffer
            {
                OfferId = 555,
                OfferNumber = 1,
                OfferTypeId = 18,
                OfferName = "Test SpendX",
                Spend1 = 50m,
                SavePct1 = 10m,
                ForecastSales = 15000m,
                ForecastCost = 9000m,
                ForecastTradeMargin = 6000m,
                ForecastTmPercent = 40.0m,
                TotalSupplierIncome = 2500m
            };

            _repoMock.Setup(r => r.GetOfferByIdAsync(555, 1))
                     .ReturnsAsync(existingOffer);

            _repoMock.Setup(r => r.UpdateOfferHeaderOverviewAsync(It.IsAny<TEventOffer>()))
                     .ReturnsAsync(true);

            var request = new SpendXOverviewRequestDto
            {
                OfferId = 555,
                OfferNo = 1,
                Overview = new SpendXOverviewDto
                {
                    OfferName = "Updated SpendX",
                    Spend1 = 60m,
                    SavePct1 = 15m
                }
            };

            // Act
            var response = await _service.SaveEventOfferOverviewAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.Equal(15000m, response.ForecastSales);
            Assert.Equal(9000m, response.ForecastCost);
            Assert.Equal(6000m, response.ForecastTmDollar);
            Assert.Equal(40.0m, response.ForecastTmPercent);
            Assert.Equal(2500m, response.TotalSupplierIncome);
        }
    }
}
