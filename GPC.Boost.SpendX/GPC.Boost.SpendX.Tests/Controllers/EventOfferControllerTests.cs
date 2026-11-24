using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using GPC.Boost.SpendX.Api.Controllers;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GPC.Boost.SpendX.Tests.Controllers
{
    public class EventOfferControllerTests
    {
        private readonly ISpendXService                          _spendXService;
        private readonly IValidator<EventOfferRequestDto>        _offerValidator;
        private readonly IValidator<SpendXOverviewRequestDto>    _overviewValidator;
        private readonly IValidator<DataViewRequestDto>          _dataViewRequestValidator;
        private readonly IValidator<DataViewResponseDto>         _dataViewResponseValidator;
        private readonly IValidator<SearchByBrandRequestDto>     _searchByBrandValidator;
        private readonly IValidator<MoveOfferRequestDto>         _moveOfferValidator;
        private readonly IValidator<PricingExtractRequestDto>    _pricingExtractValidator;
        private readonly ILogger<EventOfferController>           _logger;
        private readonly EventOfferController                    _controller;

        public EventOfferControllerTests()
        {
            _spendXService             = Substitute.For<ISpendXService>();
            _offerValidator            = Substitute.For<IValidator<EventOfferRequestDto>>();
            _overviewValidator         = Substitute.For<IValidator<SpendXOverviewRequestDto>>();
            _dataViewRequestValidator  = Substitute.For<IValidator<DataViewRequestDto>>();
            _dataViewResponseValidator = Substitute.For<IValidator<DataViewResponseDto>>();
            _searchByBrandValidator    = Substitute.For<IValidator<SearchByBrandRequestDto>>();
            _moveOfferValidator        = Substitute.For<IValidator<MoveOfferRequestDto>>();
            _pricingExtractValidator   = Substitute.For<IValidator<PricingExtractRequestDto>>();
            _logger                    = Substitute.For<ILogger<EventOfferController>>();

            _controller = new EventOfferController(
                _spendXService,
                _offerValidator,
                _overviewValidator,
                _dataViewRequestValidator,
                _dataViewResponseValidator,
                _searchByBrandValidator,
                _moveOfferValidator,
                _pricingExtractValidator,
                _logger);

            // All validators pass by default
            PassValidator(_offerValidator);
            PassValidator(_overviewValidator);
            PassValidator(_dataViewRequestValidator);
            PassValidator(_dataViewResponseValidator);
            PassValidator(_searchByBrandValidator);
            PassValidator(_moveOfferValidator);
            PassValidator(_pricingExtractValidator);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. AddEventOffer  –  POST  EventOffer/CreateEventOffer
        // ══════════════════════════════════════════════════════════════════════
        #region AddEventOffer

        [Fact]
        public async Task AddEventOffer_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId       = 1,
                OfferTypeId   = 18,
                ProductBrand  = "Samsung",
                Country       = "AU",
                Spend1        = 100m,
                SavePct1      = 5m,
                PageNumber    = 1,
                PositionId    = 1,
                UserId        = "testuser"
            };

            _spendXService.AddEventOfferAsync(Arg.Any<EventOfferRequestDto>())
                .Returns(new CreateEventOfferResponseDto { OfferId = 42, OfferNo = 1, SkuCount = 5, Error = null });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.AddEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).AddEventOfferAsync(Arg.Any<EventOfferRequestDto>());
        }

        [Fact]
        public async Task AddEventOffer_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var req = CreateMockHttpRequestData<EventOfferRequestDto>(null!);

            // Act
            var result = await _controller.AddEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().AddEventOfferAsync(Arg.Any<EventOfferRequestDto>());
        }

        [Fact]
        public async Task AddEventOffer_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new EventOfferRequestDto
            {
                EventId = 1, OfferTypeId = 18, ProductBrand = "Samsung",
                Spend1 = 100m, SavePct1 = 5m, PageNumber = 1, PositionId = 1, UserId = "testuser"
            };

            _spendXService.AddEventOfferAsync(Arg.Any<EventOfferRequestDto>())
                .Returns(new CreateEventOfferResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "SAVE_FAILED", Message = "Failed to persist offer." }
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.AddEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 2. SearchByBrand  –  GET  EventOffer/SearchByBrand
        // ══════════════════════════════════════════════════════════════════════
        #region SearchByBrand

        [Fact]
        public async Task SearchByBrand_ValidRequest_ReturnsOk()
        {
            // Arrange
            _spendXService.SearchProductsByBrandAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new List<ProductSearchResultDto>
                {
                    new ProductSearchResultDto { Sku = "SKU001", Brand = "Samsung", Country = "AU" },
                    new ProductSearchResultDto { Sku = "SKU002", Brand = "Samsung", Country = "AU" }
                });

            var req = CreateMockHttpGetRequestData("?brand=Samsung&country=AU");

            // Act
            var result = await _controller.SearchByBrand(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).SearchProductsByBrandAsync(
                Arg.Is<string>(b => b == "Samsung"),
                Arg.Is<string>(c => c == "AU"));
        }

        [Fact]
        public async Task SearchByBrand_ValidationFails_ReturnsBadRequest()
        {
            // Arrange  – brand is missing
            FailValidator(_searchByBrandValidator, "Brand query parameter is required.");
            var req = CreateMockHttpGetRequestData("?country=AU");

            // Act
            var result = await _controller.SearchByBrand(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().SearchProductsByBrandAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task SearchByBrand_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _spendXService.SearchProductsByBrandAsync(Arg.Any<string>(), Arg.Any<string>())
                .ThrowsAsync(new Exception("DB timeout"));

            var req = CreateMockHttpGetRequestData("?brand=Samsung&country=AU");

            // Act
            var result = await _controller.SearchByBrand(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 3. GetEventOffer  –  GET  EventOffer/GetEventOffer
        // ══════════════════════════════════════════════════════════════════════
        #region GetEventOffer

        [Fact]
        public async Task GetEventOffer_ValidRequest_ReturnsOk()
        {
            // Arrange
            _spendXService.GetEventOfferAsync(Arg.Any<GetEventOfferRequestDto>())
                .Returns(new SpendXOfferDto
                {
                    OfferId    = 10,
                    OfferNo    = 1,
                    Country    = "AU",
                    GST        = 0.10m,
                    Overview   = new SpendXOverviewDto { Spend1 = 100m, SavePct1 = 5m },
                    Pricing    = new SpendXPricingDto  { Spend1 = 100m, SavePct1 = 5m },
                    OfferInfo  = new SpendXOfferInfoDto(),
                    Forecasting = new SpendXForecastingDto(),
                    Tiers      = new List<SpendXTierDto>
                    {
                        new SpendXTierDto { TierNumber = 1, SpendThreshold = 100m, DiscountPercent = 5m }
                    },
                    Error = null
                });

            var req = CreateMockHttpGetRequestData("?EventId=1&OfferId=10&OfferNo=1&OfferTypeId=18&Country=AU");

            // Act
            var result = await _controller.GetEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).GetEventOfferAsync(
                Arg.Is<GetEventOfferRequestDto>(x => x.OfferId == 10));
        }

        [Fact]
        public async Task GetEventOffer_MissingOfferIdAndPagePosition_ReturnsBadRequest()
        {
            // Arrange  – controller guard: OfferId ≤ 0 AND PageId/PagePosition ≤ 0
            var req = CreateMockHttpGetRequestData("?EventId=1&OfferId=0&PageId=0&PagePosition=0&OfferTypeId=18");

            // Act
            var result = await _controller.GetEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().GetEventOfferAsync(Arg.Any<GetEventOfferRequestDto>());
        }

        [Fact]
        public async Task GetEventOffer_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            _spendXService.GetEventOfferAsync(Arg.Any<GetEventOfferRequestDto>())
                .Returns(new SpendXOfferDto
                {
                    Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = "SpendX Offer not found." }
                });

            var req = CreateMockHttpGetRequestData("?EventId=1&OfferId=999&OfferNo=1&OfferTypeId=18");

            // Act
            var result = await _controller.GetEventOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 4. SaveEventOfferOverview  –  POST  EventOffer/SaveEventOfferOverview
        // ══════════════════════════════════════════════════════════════════════
        #region SaveEventOfferOverview

        [Fact]
        public async Task SaveEventOfferOverview_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new SpendXOverviewRequestDto
            {
                OfferId    = 100,
                OfferNo    = 1,
                EventId    = 1,
                PageId     = 1,
                PositionId = 1,
                UserId     = "testuser",
                Overview   = new SpendXOverviewDto { Spend1 = 100m, SavePct1 = 5m },
                OfferInfo  = new SpendXOfferInfoDto(),
                Pricing    = new SpendXPricingDto()
            };

            _spendXService.SaveEventOfferOverviewAsync(Arg.Any<SpendXOverviewRequestDto>())
                .Returns(new SpendXOverviewSaveResponseDto
                {
                    Message           = "SpendX offer overview and merchandising flags saved successfully.",
                    ForecastSales     = 10000m,
                    ForecastCost      = 6000m,
                    ForecastTmDollar  = 4000m,
                    ForecastTmPercent = 40m,
                    Error             = null
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.SaveEventOfferOverview(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).SaveEventOfferOverviewAsync(
                Arg.Is<SpendXOverviewRequestDto>(x => x.OfferId == 100));
        }

        [Fact]
        public async Task SaveEventOfferOverview_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var req = CreateMockHttpRequestData<SpendXOverviewRequestDto>(null!);

            // Act
            var result = await _controller.SaveEventOfferOverview(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().SaveEventOfferOverviewAsync(Arg.Any<SpendXOverviewRequestDto>());
        }

        [Fact]
        public async Task SaveEventOfferOverview_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new SpendXOverviewRequestDto
            {
                OfferId    = 100,
                OfferNo    = 1,
                EventId    = 1,
                PageId     = 1,
                PositionId = 1,
                UserId     = "testuser",
                Overview   = new SpendXOverviewDto { Spend1 = 100m, SavePct1 = 5m },
                OfferInfo  = new SpendXOfferInfoDto(),
                Pricing    = new SpendXPricingDto()
            };

            _spendXService.SaveEventOfferOverviewAsync(Arg.Any<SpendXOverviewRequestDto>())
                .Returns(new SpendXOverviewSaveResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "SAVE_FAILED", Message = "Failed to update offer overview." }
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.SaveEventOfferOverview(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 5. EventOfferDataView  –  GET  EventOffer/EventOfferDataView
        // ══════════════════════════════════════════════════════════════════════
        #region EventOfferDataView

        [Fact]
        public async Task EventOfferDataView_ValidRequest_ReturnsOk()
        {
            // Arrange
            _spendXService.GetDataViewAsync(Arg.Any<DataViewRequestDto>())
                .Returns(new DataViewResponseDto
                {
                    EventId  = 1,
                    OfferId  = 10,
                    OfferNo  = 1,
                    Spend1   = 100m,
                    SavePct1 = 5m,
                    PaginationInfo = new PaginationInfo { TotalCount = 2, PageNumber = 1, PageSize = 50 },
                    EventOfferDataView = new List<OfferSkuOverviewDto>
                    {
                        new OfferSkuOverviewDto { Sku = "SKU001", CatFcst = 10 },
                        new OfferSkuOverviewDto { Sku = "SKU002", CatFcst = 20 }
                    },
                    Error = null
                });

            var req = CreateMockHttpGetRequestData(
                "?EventId=1&EventOfferId=10&OfferNo=1&OfferTypeId=18&PageNumber=1&PageSize=50");

            // Act
            var result = await _controller.EventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).GetDataViewAsync(
                Arg.Is<DataViewRequestDto>(x => x.EventId == 1 && x.EventOfferId == 10));
        }

        [Fact]
        public async Task EventOfferDataView_ValidationFails_ReturnsBadRequest()
        {
            // Arrange  – EventOfferId = 0 fails validator
            FailValidator(_dataViewRequestValidator, "EventOfferId must be greater than zero.");
            var req = CreateMockHttpGetRequestData(
                "?EventId=1&EventOfferId=0&OfferNo=1&OfferTypeId=18&PageNumber=1&PageSize=50");

            // Act
            var result = await _controller.EventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().GetDataViewAsync(Arg.Any<DataViewRequestDto>());
        }

        [Fact]
        public async Task EventOfferDataView_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            _spendXService.GetDataViewAsync(Arg.Any<DataViewRequestDto>())
                .Returns(new DataViewResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = "SpendX offer 10 not found." }
                });

            var req = CreateMockHttpGetRequestData(
                "?EventId=1&EventOfferId=10&OfferNo=1&OfferTypeId=18&PageNumber=1&PageSize=50");

            // Act
            var result = await _controller.EventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 6. PostEventOfferDataView  –  POST  EventOffer/PostEventOfferDataView
        // ══════════════════════════════════════════════════════════════════════
        #region PostEventOfferDataView

        [Fact]
        public async Task PostEventOfferDataView_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new DataViewResponseDto
            {
                EventId  = 1,
                OfferId  = 10,
                OfferNo  = 1,
                Spend1   = 100m,
                SavePct1 = 5m,
                EventOfferDataView = new List<OfferSkuOverviewDto>
                {
                    new OfferSkuOverviewDto { Sku = "SKU001", CatFcst = 10 }
                }
            };

            _spendXService.PostDataViewAsync(Arg.Any<DataViewResponseDto>())
                .Returns(new DataviewCalculationsResponseDto
                {
                    Message          = "SpendX data view updated successfully.",
                    ForecastSales    = 15000m,
                    ForecastCost     = 9000m,
                    ForecastTmDollar = 6000m,
                    Error            = null
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.PostEventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).PostDataViewAsync(Arg.Any<DataViewResponseDto>());
        }

        [Fact]
        public async Task PostEventOfferDataView_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var req = CreateMockHttpRequestData<DataViewResponseDto>(null!);

            // Act
            var result = await _controller.PostEventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().PostDataViewAsync(Arg.Any<DataViewResponseDto>());
        }

        [Fact]
        public async Task PostEventOfferDataView_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            var request = new DataViewResponseDto
            {
                EventId            = 1,
                OfferId            = 10,
                OfferNo            = 1,
                EventOfferDataView = new List<OfferSkuOverviewDto>
                {
                    new OfferSkuOverviewDto { Sku = "SKU001", CatFcst = 10 }
                }
            };

            _spendXService.PostDataViewAsync(Arg.Any<DataViewResponseDto>())
                .Returns(new DataviewCalculationsResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = "No detail records found for OfferId 10." }
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.PostEventOfferDataView(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 7. MoveOffer  –  POST  EventOffer/MoveOffer
        // ══════════════════════════════════════════════════════════════════════
        #region MoveOffer

        [Fact]
        public async Task MoveOffer_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId            = 1,
                OfferId            = 10,
                SourcePageId       = 1,
                SourcePagePosition = 2,
                TargetPageId       = 3,
                TargetPagePosition = 4
            };

            _spendXService.MoveOfferAsync(Arg.Any<MoveOfferRequestDto>())
                .Returns(new MoveOfferResponseDto
                {
                    Message = "Offer 10 moved successfully from Page: 1 and Position: 2 to Page: 3 and Position: 4.",
                    Error   = null
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.MoveOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).MoveOfferAsync(
                Arg.Is<MoveOfferRequestDto>(x =>
                    x.OfferId == 10 &&
                    x.SourcePageId == 1 && x.SourcePagePosition == 2 &&
                    x.TargetPageId == 3 && x.TargetPagePosition == 4));
        }

        [Fact]
        public async Task MoveOffer_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var req = CreateMockHttpRequestData<MoveOfferRequestDto>(null!);

            // Act
            var result = await _controller.MoveOffer(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().MoveOfferAsync(Arg.Any<MoveOfferRequestDto>());
        }

        [Fact]
        public async Task MoveOffer_ServiceReturnsNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new MoveOfferRequestDto
            {
                EventId = 1, OfferId = 999,
                SourcePageId = 1, SourcePagePosition = 1,
                TargetPageId = 2, TargetPagePosition = 2
            };

            _spendXService.MoveOfferAsync(Arg.Any<MoveOfferRequestDto>())
                .Returns(new MoveOfferResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = "Offer 999 not found in Event 1." }
                });

            var req = CreateMockHttpRequestData(request);

            // Act
            var result = await _controller.MoveOffer(req);

            // Assert
            // Controller maps NOT_FOUND → 404
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // 8. GetPricingExtract  –  GET  OutBound/GetPricingExtract
        // ══════════════════════════════════════════════════════════════════════
        #region GetPricingExtract

        [Fact]
        public async Task GetPricingExtract_ValidRequest_ReturnsOk()
        {
            // Arrange
            _spendXService.GetPricingExtractAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns(new PricingExtractResponseDto
                {
                    EventId    = 1,
                    TotalCount = 2,
                    SpendXGetYPercent = new List<SpendXPricingExtractRowDto>
                    {
                        new SpendXPricingExtractRowDto { Sku = "SKU001", Spend1 = 100m, SavePct1 = 5m, IsRewards = false }
                    },
                    SpendXGetYPercentLoyal = new List<SpendXPricingExtractRowDto>
                    {
                        new SpendXPricingExtractRowDto { Sku = "SKU002", Spend1 = 100m, SavePct1 = 8m, IsRewards = true }
                    },
                    Message = "Pricing extract generated successfully. Total records: 2 (SpendX: 1, SpendX Loyal: 1).",
                    Error   = null
                });

            var req = CreateMockHttpGetRequestData("?eventId=1&UserId=testuser");

            // Act
            var result = await _controller.GetPricingExtract(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            await _spendXService.Received(1).GetPricingExtractAsync(
                Arg.Is<int>(id => id == 1),
                Arg.Is<string?>(uid => uid == "testuser"));
        }

        [Fact]
        public async Task GetPricingExtract_ValidationFails_ReturnsBadRequest()
        {
            // Arrange  – EventId = 0 fails validator
            FailValidator(_pricingExtractValidator, "EventId must be greater than zero.");
            var req = CreateMockHttpGetRequestData("?eventId=0&UserId=testuser");

            // Act
            var result = await _controller.GetPricingExtract(req);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            await _spendXService.DidNotReceive().GetPricingExtractAsync(Arg.Any<int>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task GetPricingExtract_ServiceReturnsError_ReturnsInternalServerError()
        {
            // Arrange
            _spendXService.GetPricingExtractAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns(new PricingExtractResponseDto
                {
                    Error = new ErrorDto { ErrorCode = "PRICING_EXTRACT_ERROR", Message = "DB error while generating extract." }
                });

            var req = CreateMockHttpGetRequestData("?eventId=1&UserId=testuser");

            // Act
            var result = await _controller.GetPricingExtract(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════════════
        // Infrastructure helpers  (same pattern as the reference test file)
        // ══════════════════════════════════════════════════════════════════════
        #region Helpers

        private static void PassValidator<T>(IValidator<T> validator) where T : class =>
            validator.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>())
                     .Returns(new ValidationResult());

        private static void FailValidator<T>(IValidator<T> validator, string message) where T : class =>
            validator.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>())
                     .Returns(new ValidationResult(new[]
                     {
                         new ValidationFailure("Field", message)
                     }));

        private static readonly IServiceProvider _serviceProvider = CreateServiceProvider();

        private static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<WorkerOptions>(options =>
            {
                options.Serializer = new Azure.Core.Serialization.JsonObjectSerializer();
            });
            return services.BuildServiceProvider();
        }

        /// <summary>POST helper – serialises <typeparamref name="T"/> to JSON and wraps it in a mock request.</summary>
        private static HttpRequestData CreateMockHttpRequestData<T>(T body)
        {
            var json = body is null ? "" : JsonConvert.SerializeObject(body);
            return BuildRequest(json, "https://localhost/api/endpoint");
        }

        /// <summary>GET helper – appends <paramref name="queryString"/> to the request URL.</summary>
        private static HttpRequestData CreateMockHttpGetRequestData(string queryString)
        {
            return BuildRequest("", "https://localhost/api/endpoint" + queryString);
        }

        /// <summary>Core factory used by both helpers.</summary>
        private static HttpRequestData BuildRequest(string body, string url)
        {
            var context = Substitute.For<FunctionContext>();
            context.InstanceServices.Returns(_serviceProvider);

            // ── Response ──────────────────────────────────────────────────────
            var response = Substitute.For<HttpResponseData>(context);
            response.Body.Returns(new MemoryStream());
            response.Headers.Returns(new HttpHeadersCollection());

            HttpStatusCode capturedStatus = HttpStatusCode.OK;
            response.StatusCode = Arg.Do<HttpStatusCode>(s => capturedStatus = s);
            response.StatusCode.Returns(_ => capturedStatus);

            // ── Request ───────────────────────────────────────────────────────
            var request = Substitute.For<HttpRequestData>(context);
            request.Body.Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            request.Headers.Returns(new HttpHeadersCollection());
            request.Url.Returns(new Uri(url));
            request.CreateResponse().Returns(response);

            return request;
        }

        #endregion
    }
}
