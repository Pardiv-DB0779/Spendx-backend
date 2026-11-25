using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Domain.Models;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Repository;
using GPC.Boost.SpendX.Service;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GPC.Boost.SpendX.Tests.Services
{
    public class SpendXServiceTests
    {
        private readonly ISpendXRepository _repository;
        private readonly ILogger<SpendXService> _logger;
        private readonly SpendXService _service;

        public SpendXServiceTests()
        {
            _repository = Substitute.For<ISpendXRepository>();
            _logger = Substitute.For<ILogger<SpendXService>>();
            _service = new SpendXService(_repository, _logger);
        }

        #region AddEventOfferAsync Tests

        [Fact]
        public async Task AddEventOfferAsync_NullRequest_ReturnsInvalidRequest()
        {
            // Act
            var result = await _service.AddEventOfferAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("INVALID_REQUEST", result.Error.ErrorCode);
        }

        [Fact]
        public async Task AddEventOfferAsync_NoBrandProvided_ReturnsInvalidRequest()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = null,
                Search = new ProductSearchRequestDto { ProductBrand = new List<string>() }
            };

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("INVALID_REQUEST", result.Error.ErrorCode);
            Assert.Contains("brand must be specified", result.Error.Message);
        }

        [Fact]
        public async Task AddEventOfferAsync_NoProductsFound_ReturnsNoProductsFound()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = "NonExistentBrand",
                Country = "AU"
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(new List<TProduct>());

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NO_PRODUCTS_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task AddEventOfferAsync_NoEligibleSkusRemaining_ReturnsNoEligibleSkus()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = "Bosch",
                Country = "AU",
                UnSelectedSKUs = new List<string> { "SKU100" }
            };

            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU100", Brand = "Bosch", Country = "AU" }
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(products);

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NO_ELIGIBLE_SKUS", result.Error.ErrorCode);
        }

        [Fact]
        public async Task AddEventOfferAsync_SaveFails_ReturnsSaveFailed()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = "Bosch",
                Country = "AU",
                PositionId = 1,
                OfferTypeId = 18
            };

            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU100", Brand = "Bosch", Country = "AU" }
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(products);

            _repository.SaveOrUpdateSpendXOfferAsync(Arg.Any<TEventOffer>(), Arg.Any<List<TEventOfferDetail>>(), Arg.Any<bool>())
                .Returns((false, 0));

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("SAVE_FAILED", result.Error.ErrorCode);
        }

        [Fact]
        public async Task AddEventOfferAsync_ValidCreateRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = "Bosch",
                Country = "AU",
                PositionId = 1,
                OfferTypeId = 18,
                PageNumber = 1,
                UserId = "user1"
            };

            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU100", Brand = "Bosch", Country = "AU" }
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(products);

            _repository.SaveOrUpdateSpendXOfferAsync(Arg.Any<TEventOffer>(), Arg.Any<List<TEventOfferDetail>>(), false)
                .Returns((true, 501));

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(501, result.OfferId);
            Assert.Contains("Successfully", result.Message);
        }

        [Fact]
        public async Task AddEventOfferAsync_ValidUpdateRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                OfferId = 501,
                OfferNo = 1,
                ProductBrand = "Bosch",
                Country = "AU",
                PositionId = 1,
                OfferTypeId = 18,
                PageNumber = 1,
                UserId = "user1"
            };

            var existingOffer = new TEventOffer
            {
                OfferId = 501,
                OfferNumber = 1,
                EventId = 1
            };

            var products = new List<TProduct>
            {
                new TProduct { Sku = "SKU100", Brand = "Bosch", Country = "AU" }
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(products);

            _repository.GetOfferByIdAsync(501, 1).Returns(existingOffer);

            _repository.SaveOrUpdateSpendXOfferAsync(Arg.Any<TEventOffer>(), Arg.Any<List<TEventOfferDetail>>(), true)
                .Returns((true, 501));

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(501, result.OfferId);
            Assert.Contains("updated Successfully", result.Message);
        }

        [Fact]
        public async Task AddEventOfferAsync_ExceptionThrown_ReturnsInternalError()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1,
                ProductBrand = "Bosch"
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), Arg.Any<string>(), null)
                .ThrowsAsync(new Exception("Database connection failure"));

            // Act
            var result = await _service.AddEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("INTERNAL_ERROR", result.Error.ErrorCode);
        }

        #endregion

        #region GetEventOfferAsync Tests

        [Fact]
        public async Task GetEventOfferAsync_OfferNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new GetEventOfferRequestDto { OfferId = 999, PagePosition = 1 };
            _repository.GetOfferByIdAsync(999, 1).Returns((TEventOffer)null!);

            // Act
            var result = await _service.GetEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NOT_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task GetEventOfferAsync_ValidOffer_ReturnsSpendXOfferDto()
        {
            // Arrange
            var request = new GetEventOfferRequestDto { OfferId = 100, PagePosition = 2 };
            var offer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                Page = 1,
                PagePosition = 2,
                Country = "AU",
                OfferType = "SPEND X GET Y% OFF",
                Brand = "Bosch",
                Spend1 = 50m,
                SavePct1 = 10m,
                Spend2 = 100m,
                SavePct2 = 20m
            };

            _repository.GetOfferByIdAsync(100, 1).Returns(offer);
            _repository.GetOfferDetailSummaryAsync(100, 1).Returns(("PART123", 10m, 20m));

            // Act
            var result = await _service.GetEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(100, result.OfferId);
            Assert.Equal(0.10m, result.GST); // AU GST
            Assert.NotNull(result.Overview);
            Assert.Equal("Bosch", result.Overview.Brand);
            Assert.Equal(2, result.Tiers.Count); // Tier 1 and Tier 2
        }

        [Fact]
        public async Task GetEventOfferAsync_NZCountry_Applies15PercentGST()
        {
            // Arrange
            var request = new GetEventOfferRequestDto { OfferId = 100, PagePosition = 1 };
            var offer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                Page = 1,
                PagePosition = 1,
                Country = "NZ",
                Spend1 = 50m,
                SavePct1 = 10m
            };

            _repository.GetOfferByIdAsync(100, 1).Returns(offer);
            _repository.GetOfferDetailSummaryAsync(100, 1).Returns(("PART-NZ", 5m, 15m));

            // Act
            var result = await _service.GetEventOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.15m, result.GST); // NZ GST is 15%
        }

        #endregion

        #region SaveEventOfferOverviewAsync Tests

        [Fact]
        public async Task SaveEventOfferOverviewAsync_NullRequest_ReturnsInvalidRequest()
        {
            // Act
            var result = await _service.SaveEventOfferOverviewAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("INVALID_REQUEST", result.Error.ErrorCode);
        }

        [Fact]
        public async Task SaveEventOfferOverviewAsync_OfferNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new SpendXOverviewRequestDto
            {
                OfferId = 999,
                OfferNo = 1,
                Overview = new SpendXOverviewDto { Spend1 = 100m, SavePct1 = 10m }
            };

            _repository.GetOfferByIdAsync(999, 1).Returns((TEventOffer)null!);

            // Act
            var result = await _service.SaveEventOfferOverviewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NOT_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task SaveEventOfferOverviewAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new SpendXOverviewRequestDto
            {
                OfferId = 100,
                OfferNo = 1,
                Overview = new SpendXOverviewDto
                {
                    OfferName = "Updated Name",
                    Spend1 = 150m,
                    SavePct1 = 15m
                }
            };

            var existingOffer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                Spend1 = 100m,
                SavePct1 = 10m
            };

            var updatedOffer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                ForecastSales = 5000m,
                ForecastCost = 3000m,
                ForecastTradeMargin = 2000m,
                ForecastTmPercent = 40m,
                TotalSupplierIncome = 250m
            };

            _repository.GetOfferByIdAsync(100, 1).Returns(existingOffer, updatedOffer);
            _repository.UpdateOfferHeaderOverviewAsync(Arg.Any<TEventOffer>(), Arg.Any<bool?>()).Returns(true);

            // Act
            var result = await _service.SaveEventOfferOverviewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(5000m, result.ForecastSales);
            Assert.Equal(3000m, result.ForecastCost);
            Assert.Equal(2000m, result.ForecastTmDollar);
            Assert.Equal(40m, result.ForecastTmPercent);
        }

        [Fact]
        public async Task SaveEventOfferOverviewAsync_UpdateFails_ReturnsSaveFailed()
        {
            // Arrange
            var request = new SpendXOverviewRequestDto
            {
                OfferId = 100,
                OfferNo = 1,
                Overview = new SpendXOverviewDto { Spend1 = 150m, SavePct1 = 15m }
            };

            var existingOffer = new TEventOffer { OfferId = 100, OfferNumber = 1 };
            _repository.GetOfferByIdAsync(100, 1).Returns(existingOffer);
            _repository.UpdateOfferHeaderOverviewAsync(Arg.Any<TEventOffer>(), Arg.Any<bool?>()).Returns(false);

            // Act
            var result = await _service.SaveEventOfferOverviewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("SAVE_FAILED", result.Error.ErrorCode);
        }

        #endregion

        #region SearchProductsByBrandAsync Tests

        [Fact]
        public async Task SearchProductsByBrandAsync_ValidBrands_ReturnsMappedResults()
        {
            // Arrange
            var products = new List<TProduct>
            {
                new TProduct
                {
                    Sku = "SKU-01",
                    PartNo = "PART-01",
                    Brand = "Bosch",
                    Description = "Spark Plug",
                    Country = "AU",
                    EverydayPrice = 12.50m,
                    EverydayPriceGst = 13.75m
                }
            };

            _repository.GetProductsByBrandsAsync(Arg.Any<List<string>>(), "AU", null)
                .Returns(products);

            // Act
            var result = await _service.SearchProductsByBrandAsync("Bosch", "AU");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("SKU-01", result[0].Sku);
            Assert.Equal("PART-01", result[0].PartNumber);
            Assert.Equal(12.50m, result[0].EverydayPrice);
        }

        #endregion

        #region GetDataViewAsync Tests

        [Fact]
        public async Task GetDataViewAsync_NullRequest_ReturnsInvalidRequest()
        {
            // Act
            var result = await _service.GetDataViewAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("INVALID_REQUEST", result.Error.ErrorCode);
        }

        [Fact]
        public async Task GetDataViewAsync_OfferNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new DataViewRequestDto { EventOfferId = 999, OfferNo = 1 };
            _repository.GetOfferByIdAsync(999, 1).Returns((TEventOffer)null!);

            // Act
            var result = await _service.GetDataViewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NOT_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task GetDataViewAsync_ValidRequest_ReturnsDataViewResponse()
        {
            // Arrange
            var request = new DataViewRequestDto { EventId = 1, EventOfferId = 100, OfferNo = 1, PageNumber = 1, PageSize = 10 };
            var offer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                Page = 1,
                PagePosition = 1,
                OfferTypeId = 18,
                Spend1 = 100m,
                SavePct1 = 10m
            };

            var items = new List<OfferSkuOverviewDto>
            {
                new OfferSkuOverviewDto { Sku = "SKU-01", Brand = "Bosch" }
            };

            _repository.GetOfferByIdAsync(100, 1).Returns(offer);
            _repository.GetDataViewAsync(request).Returns((items, 1));

            // Act
            var result = await _service.GetDataViewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(100, result.OfferId);
            Assert.Single(result.EventOfferDataView);
            Assert.Equal(1, result.PaginationInfo?.TotalCount);
        }

        #endregion

        #region PostDataViewAsync Tests

        [Fact]
        public async Task PostDataViewAsync_NullOrEmptyData_ReturnsEmptyDataError()
        {
            // Arrange
            var request = new DataViewResponseDto { EventOfferDataView = new List<OfferSkuOverviewDto>() };

            // Act
            var result = await _service.PostDataViewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("EMPTY_DATA", result.Error.ErrorCode);
        }

        [Fact]
        public async Task PostDataViewAsync_DetailsNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new DataViewResponseDto
            {
                OfferId = 100,
                OfferNo = 1,
                EventOfferDataView = new List<OfferSkuOverviewDto>
                {
                    new OfferSkuOverviewDto { Sku = "SKU-01" }
                }
            };

            _repository.GetOfferDetailsAsync(100, 1).Returns(new List<TEventOfferDetail>());

            // Act
            var result = await _service.PostDataViewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NOT_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task PostDataViewAsync_ValidRequest_CalculatesAndSaves()
        {
            // Arrange
            var request = new DataViewResponseDto
            {
                OfferId = 100,
                OfferNo = 1,
                OfferTypeId = 18,
                EventOfferDataView = new List<OfferSkuOverviewDto>
                {
                    new OfferSkuOverviewDto
                    {
                        Sku = "SKU-01",
                        CatFcst = 10,
                        IncrFcst = 2,
                        DisplayInd = true
                    }
                }
            };

            var details = new List<TEventOfferDetail>
            {
                new TEventOfferDetail
                {
                    OfferId = 100,
                    OfferNo = 1,
                    Sku = "SKU-01",
                    EverydayPrice = 50m,
                    AdvertisedPrice = 45m,
                    NationalAverageCost = 30m
                }
            };

            var updatedOffer = new TEventOffer
            {
                OfferId = 100,
                OfferNumber = 1,
                ForecastSales = 450m,
                ForecastCost = 300m,
                ForecastTradeMargin = 150m,
                ForecastTmPercent = 33.33m,
                TotalSupplierIncome = 50m
            };

            _repository.GetOfferDetailsAsync(100, 1).Returns(details);
            _repository.SaveDataViewDetailsAndRecalculateHeaderAsync(details, 100, 1, 18).Returns(true);
            _repository.GetOfferByIdAsync(100, 1).Returns(updatedOffer);

            // Act
            var result = await _service.PostDataViewAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(450m, result.ForecastSales);
            Assert.Equal(300m, result.ForecastCost);
            Assert.Equal(150m, result.ForecastTmDollar);
            await _repository.Received(1).SaveDataViewDetailsAndRecalculateHeaderAsync(details, 100, 1, 18);
        }

        #endregion

        #region MoveOfferAsync Tests

        [Fact]
        public async Task MoveOfferAsync_OfferNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId = 1,
                OfferId = 999,
                SourcePageId = 1,
                SourcePagePosition = 1,
                TargetPageId = 2,
                TargetPagePosition = 3
            };

            _repository.GetOfferByIdAsync(request.OfferId).Returns((TEventOffer)null!);

            // Act
            var result = await _service.MoveOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("NOT_FOUND", result.Error.ErrorCode);
        }

        [Fact]
        public async Task MoveOfferAsync_ValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId = 1,
                OfferId = 100,
                SourcePageId = 1,
                SourcePagePosition = 1,
                TargetPageId = 2,
                TargetPagePosition = 3
            };

            var offer = new TEventOffer
            {
                OfferId = 100,
                EventId = 1,
                Page = 1,
                PagePosition = 1
            };

            _repository.GetOfferByIdAsync(request.OfferId).Returns(offer);
            _repository.MoveOfferTransactionAsync(request).Returns(true);

            // Act
            var result = await _service.MoveOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Contains("moved successfully", result.Message);
            await _repository.Received(1).MoveOfferTransactionAsync(request);
        }

        [Fact]
        public async Task MoveOfferAsync_UpdateFails_ReturnsUpdateFailed()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId = 1,
                OfferId = 100,
                SourcePageId = 1,
                SourcePagePosition = 1,
                TargetPageId = 2,
                TargetPagePosition = 3
            };

            var offer = new TEventOffer { OfferId = 100, EventId = 1 };
            _repository.GetOfferByIdAsync(request.OfferId).Returns(offer);
            _repository.MoveOfferTransactionAsync(request).Returns(false);

            // Act
            var result = await _service.MoveOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("UPDATE_FAILED", result.Error.ErrorCode);
        }

        [Fact]
        public async Task MoveOfferAsync_ExceptionThrown_ReturnsServerError()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId = 1,
                OfferId = 100,
                SourcePageId = 1,
                SourcePagePosition = 1,
                TargetPageId = 2,
                TargetPagePosition = 3
            };

            _repository.GetOfferByIdAsync(request.OfferId).ThrowsAsync(new Exception("Database connectivity lost"));

            // Act
            var result = await _service.MoveOfferAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("SERVER_ERROR", result.Error.ErrorCode);
        }

        #endregion

        #region GetPricingExtractAsync Tests

        [Fact]
        public async Task GetPricingExtractAsync_ValidEvent_ReturnsPartitionedRows()
        {
            // Arrange
            int eventId = 10;
            var rows = new List<SpendXPricingExtractRowDto>
            {
                new SpendXPricingExtractRowDto { Sku = "SKU01", IsRewards = false, Spend1 = 100, SavePct1 = 10 },
                new SpendXPricingExtractRowDto { Sku = "SKU02", IsRewards = true, Spend1 = 100, SavePct1 = 15 }
            };

            _repository.GetPricingExtractAsync(eventId).Returns(rows);

            // Act
            var result = await _service.GetPricingExtractAsync(eventId, "user1");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Error);
            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.SpendXGetYPercent);
            Assert.Single(result.SpendXGetYPercentLoyal);
            Assert.Equal("SKU01", result.SpendXGetYPercent[0].Sku);
            Assert.Equal("SKU02", result.SpendXGetYPercentLoyal[0].Sku);
        }

        [Fact]
        public async Task GetPricingExtractAsync_ExceptionThrown_ReturnsPricingExtractError()
        {
            // Arrange
            int eventId = 10;
            _repository.GetPricingExtractAsync(eventId).ThrowsAsync(new Exception("Query timeout"));

            // Act
            var result = await _service.GetPricingExtractAsync(eventId, "user1");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Error);
            Assert.Equal("PRICING_EXTRACT_ERROR", result.Error.ErrorCode);
        }

        #endregion
    }
}
