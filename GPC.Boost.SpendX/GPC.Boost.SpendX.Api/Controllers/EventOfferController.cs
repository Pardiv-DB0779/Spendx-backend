using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using FluentValidation;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using GPC.Boost.SpendX.Service;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GPC.Boost.SpendX.Api.Controllers
{
    public class EventOfferController
    {
        private readonly ISpendXService _spendXService;
        private readonly IValidator<EventOfferRequestDto> _offerValidator;
        private readonly IValidator<SpendXOverviewRequestDto> _overviewValidator;
        private readonly IValidator<DataViewRequestDto> _dataViewRequestValidator;
        private readonly IValidator<DataViewResponseDto> _dataViewResponseValidator;
        private readonly IValidator<SearchByBrandRequestDto> _searchByBrandValidator;
        private readonly ILogger<EventOfferController> _logger;

        public EventOfferController(
            ISpendXService spendXService,
            IValidator<EventOfferRequestDto> offerValidator,
            IValidator<SpendXOverviewRequestDto> overviewValidator,
            IValidator<DataViewRequestDto> dataViewRequestValidator,
            IValidator<DataViewResponseDto> dataViewResponseValidator,
            IValidator<SearchByBrandRequestDto> searchByBrandValidator,
            ILogger<EventOfferController> logger)
        {
            _spendXService = spendXService;
            _offerValidator = offerValidator;
            _overviewValidator = overviewValidator;
            _dataViewRequestValidator = dataViewRequestValidator;
            _dataViewResponseValidator = dataViewResponseValidator;
            _searchByBrandValidator = searchByBrandValidator;
            _logger = logger;
        }

        [Function("AddEventOffer")]
        public async Task<HttpResponseData> AddEventOffer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/CreateEventOffer")] HttpRequestData req)
        {
            _logger.LogInformation("AddEventOffer triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<EventOfferRequestDto>(body);

                if (request == null)
                {
                    await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                
                var valResult = await _offerValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    await response.WriteAsJsonAsync(new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var result = await _spendXService.AddEventOfferAsync(request);
                if (result.Error != null)
                {
                    await response.WriteAsJsonAsync(result);
                    response.StatusCode = MapErrorToStatusCode(result.Error.ErrorCode);
                    return response;
                }

                await response.WriteAsJsonAsync(result);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in AddEventOffer: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        [Function("SearchByBrand")]
        public async Task<HttpResponseData> SearchByBrand(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/SearchByBrand")] HttpRequestData req)
        {
            _logger.LogInformation("SearchByBrand triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var request = new SearchByBrandRequestDto
                {
                    Brand = query["brand"] ?? query["ProductBrand"] ?? string.Empty,
                    Country = query["country"] ?? "AU"
                };

                var valResult = await _searchByBrandValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    await response.WriteAsJsonAsync(new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var results = await _spendXService.SearchProductsByBrandAsync(request.Brand, request.Country);
                await response.WriteAsJsonAsync(results);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SearchByBrand: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        [Function("GetEventOffer")]
        public async Task<HttpResponseData> GetEventOffer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/GetEventOffer")] HttpRequestData req)
        {
            _logger.LogInformation("GetEventOffer triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var request = new GetEventOfferRequestDto
                {
                    EventId = int.TryParse(query["EventId"], out var eId) ? eId : 0,
                    OfferId = int.TryParse(query["OfferId"], out var oId) ? oId : null,
                    OfferNo = int.TryParse(query["OfferNo"], out var oNo) ? oNo : 1,
                    PageId = int.TryParse(query["PageId"], out var pId) ? pId : 0,
                    PagePosition = int.TryParse(query["PagePosition"], out var pPos) ? pPos : 0,
                    OfferTypeId = int.TryParse(query["OfferTypeId"], out var otId) ? otId : 18,
                    Country = query["Country"] ?? "AU"
                };

                if ((!request.OfferId.HasValue || request.OfferId.Value <= 0) && (request.PagePosition <= 0 || request.PageId <= 0))
                {
                    await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "OfferId or valid PageId and PagePosition is required." });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var result = await _spendXService.GetEventOfferAsync(request);
                if (result.Error != null)
                {
                    await response.WriteAsJsonAsync(result);
                    response.StatusCode = MapErrorToStatusCode(result.Error.ErrorCode);
                    return response;
                }

                await response.WriteAsJsonAsync(result);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetEventOffer: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        [Function("SaveEventOfferOverview")]
        public async Task<HttpResponseData> SaveEventOfferOverview(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/SaveEventOfferOverview")] HttpRequestData req)
        {
            _logger.LogInformation("SaveEventOfferOverviewAsync triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<SpendXOverviewRequestDto>(body);

                if (request == null)
                {
                    await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var valResult = await _overviewValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    await response.WriteAsJsonAsync(new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var result = await _spendXService.SaveEventOfferOverviewAsync(request);
                if (result.Error != null)
                {
                    await response.WriteAsJsonAsync(result);
                    response.StatusCode = MapErrorToStatusCode(result.Error.ErrorCode);
                    return response;
                }

                await response.WriteAsJsonAsync(result);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SaveEventOfferOverviewAsync: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        [Function("EventOfferDataView")]
        public async Task<HttpResponseData> EventOfferDataView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/EventOfferDataView")] HttpRequestData req)
        {
            _logger.LogInformation("EventOfferDataView triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var query = HttpUtility.ParseQueryString(req.Url.Query);
                var request = new DataViewRequestDto
                {
                    EventId = int.TryParse(query["EventId"], out var eventId) ? eventId : 0,
                    EventOfferId = int.TryParse(query["EventOfferId"] ?? query["OfferId"], out var eventOfferId) ? eventOfferId : 0,
                    OfferNo = int.TryParse(query["OfferNo"], out var offerNo) ? offerNo : 1,
                    page = int.TryParse(query["page"] ?? query["PageId"], out var page) ? page : 0,
                    pagePosn = int.TryParse(query["pagePosn"] ?? query["PositionId"] ?? query["PagePosition"], out var pagePosn) ? pagePosn : 0,
                    OfferTypeId = int.TryParse(query["OfferTypeId"], out var otId) ? otId : 18,
                    OfferType = query["OfferType"],
                    IsTiered = bool.TryParse(query["isTiered"] ?? query["IsTiered"], out var isTiered) ? isTiered : null,
                    Spend1 = decimal.TryParse(query["Spend1"], out var s1) ? s1 : null,
                    SavePct1 = decimal.TryParse(query["SavePct1"] ?? query["SavePercent"], out var sv1) ? sv1 : null,
                    Spend2 = decimal.TryParse(query["Spend2"], out var s2) ? s2 : null,
                    SavePct2 = decimal.TryParse(query["SavePct2"], out var sv2) ? sv2 : null,
                    Spend3 = decimal.TryParse(query["Spend3"], out var s3) ? s3 : null,
                    SavePct3 = decimal.TryParse(query["SavePct3"], out var sv3) ? sv3 : null,
                    SavePercent = decimal.TryParse(query["SavePercent"], out var savePct) ? savePct : null,
                    IncrementalPercent = decimal.TryParse(query["incrementalPercent"] ?? query["IncrementalPercent"], out var incPct) ? incPct : null,
                    SpacePurchase = decimal.TryParse(query["SpacePurchase"], out var sp) ? sp : null,
                    PageNumber = int.TryParse(query["PageNumber"], out var pageNo) ? pageNo : 1,
                    PageSize = int.TryParse(query["PageSize"], out var pageSize) ? pageSize : 100,
                    SKUs = query["SKUs"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                    Brands = query["Brands"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                    PartNumbers = query["PartNumbers"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                    Descriptions = query["Descriptions"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                    SortOnEdPriceDesc = bool.TryParse(query["SortOnEdPriceDesc"], out var sEd) ? sEd : null,
                    SortOnCatFcstDesc = bool.TryParse(query["SortOnCatFcstDesc"], out var sCat) ? sCat : null,
                    isOfferEdited = bool.TryParse(query["isOfferEdited"], out var isEdited) ? isEdited : false
                };

                var valResult = await _dataViewRequestValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    await response.WriteAsJsonAsync(new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var result = await _spendXService.GetDataViewAsync(request);
                if (result.Error != null)
                {
                    await response.WriteAsJsonAsync(result);
                    response.StatusCode = MapErrorToStatusCode(result.Error.ErrorCode);
                    return response;
                }

                await response.WriteAsJsonAsync(result);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in EventOfferDataView: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        [Function("PostEventOfferDataView")]
        public async Task<HttpResponseData> PostEventOfferDataView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/PostEventOfferDataView")] HttpRequestData req)
        {
            _logger.LogInformation("PostEventOfferDataView triggered for SpendX");
            var response = req.CreateResponse();

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<DataViewResponseDto>(body);

                if (request == null)
                {
                    await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var valResult = await _dataViewResponseValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    await response.WriteAsJsonAsync(new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                var result = await _spendXService.PostDataViewAsync(request);
                if (result.Error != null)
                {
                    await response.WriteAsJsonAsync(result);
                    response.StatusCode = MapErrorToStatusCode(result.Error.ErrorCode);
                    return response;
                }

                await response.WriteAsJsonAsync(result);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in PostEventOfferDataView: {Message}", ex.Message);
                await response.WriteAsJsonAsync(new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = ex.Message });
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
        }

        private static HttpStatusCode MapErrorToStatusCode(string? errorCode)
        {
            return errorCode?.ToUpperInvariant() switch
            {
                "NOT_FOUND" => HttpStatusCode.NotFound,
                "BAD_REQUEST" or "INVALID_REQUEST" or "EMPTY_DATA" or "VALIDATION_FAILED" or "NO_PRODUCTS_FOUND" or "NO_ELIGIBLE_SKUS" => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
