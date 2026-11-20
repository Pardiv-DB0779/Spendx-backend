using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Api.Controllers;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Service;
using GPC.Boost.SpendX.Tests.Helpers;
using GPC.Boost.SpendX.Validation.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace GPC.Boost.SpendX.Tests
{
    public class EventOfferControllerTests
    {
        private readonly Mock<ISpendXService> _serviceMock;
        private readonly Mock<ILogger<EventOfferController>> _loggerMock;
        private readonly EventOfferRequestDtoValidator _offerValidator;
        private readonly SpendXOverviewRequestDtoValidator _overviewValidator;
        private readonly EventOfferController _controller;

        public EventOfferControllerTests()
        {
            _serviceMock = new Mock<ISpendXService>();
            _loggerMock = new Mock<ILogger<EventOfferController>>();
            _offerValidator = new EventOfferRequestDtoValidator();
            _overviewValidator = new SpendXOverviewRequestDtoValidator();

            _controller = new EventOfferController(
                _serviceMock.Object,
                _offerValidator,
                _overviewValidator,
                _loggerMock.Object);
        }

        private static FakeHttpRequestData CreateFakeRequest(string jsonBody, string url = "https://localhost/api/EventOffer", string method = "POST")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
            return new FakeHttpRequestData(FakeHttpRequestData.CreateDefaultFunctionContext(), stream, new Uri(url), method);
        }

        #region POST /api/EventOffer/CreateEventOffer Tests

        [Fact]
        public async Task AddEventOffer_ValidRequest_Returns200Ok()
        {
            // Arrange
            var requestDto = new EventOfferRequestDto
            {
                EventId = 1,
                OfferTypeId = 18,
                StartDate = "2026-11-01",
                EndDate = "2026-11-30",
                UserId = "USR001",
                OfferName = "Valvoline Promo",
                Details = new System.Collections.Generic.List<EventOfferDetailItemDto>
                {
                    new EventOfferDetailItemDto { SKU = "SKU001", PartNumber = "PN001" }
                }
            };

            _serviceMock.Setup(s => s.AddEventOfferAsync(It.IsAny<EventOfferRequestDto>()))
                        .ReturnsAsync(new CreateEventOfferResponseDto
                        {
                            OfferId = 101,
                            OfferNo = 1,
                            SkuCount = 15,
                            Message = "Event Offer Created Successfully"
                        });

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(requestDto));

            // Act
            var response = await _controller.AddEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddEventOffer_MissingUserId_Returns400BadRequest()
        {
            // Arrange
            var requestDto = new EventOfferRequestDto
            {
                EventId = 1,
                OfferTypeId = 18,
                StartDate = "2026-11-01",
                EndDate = "2026-11-30",
                UserId = null
            };

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(requestDto));

            // Act
            var response = await _controller.AddEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddEventOffer_InvalidDateRange_Returns400BadRequest()
        {
            // Arrange
            var requestDto = new EventOfferRequestDto
            {
                EventId = 1,
                OfferTypeId = 18,
                StartDate = "2026-11-30",
                EndDate = "2026-11-01",
                UserId = "USR001"
            };

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(requestDto));

            // Act
            var response = await _controller.AddEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddEventOffer_DetailMissingPartNumber_Returns400BadRequest()
        {
            // Arrange - When Details provided, SKU and PartNumber are required
            var requestDto = new EventOfferRequestDto
            {
                EventId = 1,
                OfferTypeId = 18,
                StartDate = "2026-11-01",
                EndDate = "2026-11-30",
                UserId = "USR001",
                Details = new System.Collections.Generic.List<EventOfferDetailItemDto>
                {
                    new EventOfferDetailItemDto { SKU = "SKU001", PartNumber = null }
                }
            };

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(requestDto));

            // Act
            var response = await _controller.AddEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddEventOffer_ServiceError_Returns500InternalServerError()
        {
            // Arrange
            var requestDto = new EventOfferRequestDto
            {
                EventId = 1,
                OfferTypeId = 18,
                StartDate = "2026-11-01",
                EndDate = "2026-11-30",
                UserId = "USR001"
            };

            _serviceMock.Setup(s => s.AddEventOfferAsync(It.IsAny<EventOfferRequestDto>()))
                        .ReturnsAsync(new CreateEventOfferResponseDto
                        {
                            Error = new ErrorDto { ErrorCode = "DB_ERROR", Message = "Database connection timed out" }
                        });

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(requestDto));

            // Act
            var response = await _controller.AddEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        #endregion

        #region GET /api/EventOffer/GetEventOffer Tests

        [Fact]
        public async Task GetEventOffer_ValidOfferId_Returns200Ok()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetEventOfferAsync(It.IsAny<GetEventOfferRequestDto>()))
                        .ReturnsAsync(new SpendXOfferDto
                        {
                            OfferId = 202,
                            OfferNo = 1,
                            Overview = new SpendXOverviewDto { OfferName = "Existing Offer" }
                        });

            var httpRequest = CreateFakeRequest("", url: "https://localhost/api/EventOffer/GetEventOffer?EventId=1&OfferId=202&OfferNo=1", method: "GET");

            // Act
            var response = await _controller.GetEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEventOffer_ValidGridCoordinates_Returns200Ok()
        {
            // Arrange - Lookup by PageId and PagePosition fallback
            _serviceMock.Setup(s => s.GetEventOfferAsync(It.IsAny<GetEventOfferRequestDto>()))
                        .ReturnsAsync(new SpendXOfferDto
                        {
                            OfferId = 303,
                            PageId = 2,
                            PositionId = 4
                        });

            var httpRequest = CreateFakeRequest("", url: "https://localhost/api/EventOffer/GetEventOffer?EventId=1&PageId=2&PagePosition=4", method: "GET");

            // Act
            var response = await _controller.GetEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetEventOffer_MissingCoordinatesAndOfferId_Returns400BadRequest()
        {
            // Arrange - Both OfferId and PagePosition are omitted/zero
            var httpRequest = CreateFakeRequest("", url: "https://localhost/api/EventOffer/GetEventOffer?EventId=1", method: "GET");

            // Act
            var response = await _controller.GetEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetEventOffer_OfferNotFound_Returns404NotFound()
        {
            // Arrange - Service returns error not found
            _serviceMock.Setup(s => s.GetEventOfferAsync(It.IsAny<GetEventOfferRequestDto>()))
                        .ReturnsAsync(new SpendXOfferDto
                        {
                            Error = new ErrorDto { ErrorCode = "NOT_FOUND", Message = "Offer not found." }
                        });

            var httpRequest = CreateFakeRequest("", url: "https://localhost/api/EventOffer/GetEventOffer?EventId=1&OfferId=9999", method: "GET");

            // Act
            var response = await _controller.GetEventOffer(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region POST /api/EventOffer/SaveEventOfferOverview Tests

        [Fact]
        public async Task SaveEventOfferOverview_ValidPayload_Returns200Ok()
        {
            // Arrange
            var overviewDto = new SpendXOverviewRequestDto
            {
                OfferId = 404,
                OfferNo = 1,
                Overview = new SpendXOverviewDto
                {
                    OfferName = "Updated Name",
                    Spend1 = 50m,
                    SavePct1 = 10m
                }
            };

            _serviceMock.Setup(s => s.SaveEventOfferOverviewAsync(It.IsAny<SpendXOverviewRequestDto>()))
                        .ReturnsAsync(new SpendXOverviewSaveResponseDto
                        {
                            ForecastSales = 12000m,
                            ForecastCost = 7000m,
                            ForecastTmDollar = 5000m,
                            ForecastTmPercent = 41.67m,
                            TotalSupplierIncome = 2000m
                        });

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(overviewDto));

            // Act
            var response = await _controller.SaveEventOfferOverview(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SaveEventOfferOverview_InvalidOfferId_Returns400BadRequest()
        {
            // Arrange - OfferId is 0
            var overviewDto = new SpendXOverviewRequestDto
            {
                OfferId = 0
            };

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(overviewDto));

            // Act
            var response = await _controller.SaveEventOfferOverview(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SaveEventOfferOverview_InvalidTierSequence_Returns400BadRequest()
        {
            // Arrange - Tier 2 Save % (5%) is less than Tier 1 Save % (10%)
            var overviewDto = new SpendXOverviewRequestDto
            {
                OfferId = 404,
                Overview = new SpendXOverviewDto
                {
                    Spend1 = 50m,
                    SavePct1 = 10m,
                    Spend2 = 100m,
                    SavePct2 = 5m
                }
            };

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(overviewDto));

            // Act
            var response = await _controller.SaveEventOfferOverview(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SaveEventOfferOverview_ServiceError_Returns500InternalServerError()
        {
            // Arrange
            var overviewDto = new SpendXOverviewRequestDto
            {
                OfferId = 404,
                Overview = new SpendXOverviewDto
                {
                    Spend1 = 50m,
                    SavePct1 = 10m
                }
            };

            _serviceMock.Setup(s => s.SaveEventOfferOverviewAsync(It.IsAny<SpendXOverviewRequestDto>()))
                        .ReturnsAsync(new SpendXOverviewSaveResponseDto
                        {
                            Error = new ErrorDto { ErrorCode = "SAVE_FAILED", Message = "Failed to update record" }
                        });

            var httpRequest = CreateFakeRequest(JsonConvert.SerializeObject(overviewDto));

            // Act
            var response = await _controller.SaveEventOfferOverview(httpRequest);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        #endregion
    }
}
