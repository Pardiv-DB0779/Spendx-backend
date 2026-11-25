using System;
using System.Collections.Generic;
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
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
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
        private readonly IValidator<MoveOfferRequestDto> _moveOfferValidator;
        private readonly IValidator<PricingExtractRequestDto> _pricingExtractValidator;
        private readonly ILogger<EventOfferController> _logger;

        public EventOfferController(
            ISpendXService spendXService,
            IValidator<EventOfferRequestDto> offerValidator,
            IValidator<SpendXOverviewRequestDto> overviewValidator,
            IValidator<DataViewRequestDto> dataViewRequestValidator,
            IValidator<DataViewResponseDto> dataViewResponseValidator,
            IValidator<SearchByBrandRequestDto> searchByBrandValidator,
            IValidator<MoveOfferRequestDto> moveOfferValidator,
            IValidator<PricingExtractRequestDto> pricingExtractValidator,
            ILogger<EventOfferController> logger)
        {
            _spendXService = spendXService;
            _offerValidator = offerValidator;
            _overviewValidator = overviewValidator;
            _dataViewRequestValidator = dataViewRequestValidator;
            _dataViewResponseValidator = dataViewResponseValidator;
            _searchByBrandValidator = searchByBrandValidator;
            _moveOfferValidator = moveOfferValidator;
            _pricingExtractValidator = pricingExtractValidator;
            _logger = logger;
        }

        [Function("AddEventOffer")]
        [OpenApiOperation(operationId: "AddEventOffer", tags: new[] { "EventOffer" }, Summary = "Create or update a SpendX event offer", Description = "Creates or updates a SpendX offer (Spend X Get Y% Off / Loyal) and calculates initial detail lines.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(EventOfferRequestDto), Required = true, Description = "SpendX event offer creation/update request")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CreateEventOfferResponseDto), Description = "Offer created/updated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed or invalid request")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> AddEventOffer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/CreateEventOffer")] HttpRequestData req)
        {
            _logger.LogInformation("AddEventOffer triggered for SpendX");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<EventOfferRequestDto>(body);

                if (request == null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                }

                var valResult = await _offerValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.AddEventOfferAsync(request);
                if (result.Error != null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in AddEventOffer: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while processing the request." });
            }
        }

        [Function("SearchByBrand")]
        [OpenApiOperation(operationId: "SearchByBrand", tags: new[] { "EventOffer" }, Summary = "Search products by brand", Description = "Searches active products in tproducts matching the given brand(s) and country.")]
        [OpenApiParameter(name: "brand", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Comma-separated product brand(s)")]
        [OpenApiParameter(name: "country", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Country code (AU or NZ, default AU)")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<ProductSearchResultDto>), Description = "Matching products")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> SearchByBrand(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/SearchByBrand")] HttpRequestData req)
        {
            _logger.LogInformation("SearchByBrand triggered for SpendX");

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
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var results = await _spendXService.SearchProductsByBrandAsync(request.Brand, request.Country);
                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SearchByBrand: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while searching products by brand." });
            }
        }

        [Function("GetEventOffer")]
        [OpenApiOperation(operationId: "GetEventOffer", tags: new[] { "EventOffer" }, Summary = "Get SpendX event offer details", Description = "Retrieves the SpendX offer overview, tiers, forecasting, pricing, and merchandising callouts.")]
        [OpenApiParameter(name: "EventId", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Event ID")]
        [OpenApiParameter(name: "OfferId", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Offer ID")]
        [OpenApiParameter(name: "OfferNo", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Offer Number (default 1)")]
        [OpenApiParameter(name: "PageId", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page ID")]
        [OpenApiParameter(name: "PagePosition", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page Position")]
        [OpenApiParameter(name: "OfferTypeId", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Offer Type ID (18 or 118)")]
        [OpenApiParameter(name: "Country", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Country code (AU or NZ)")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SpendXOfferDto), Description = "SpendX offer details")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Invalid parameters")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> GetEventOffer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/GetEventOffer")] HttpRequestData req)
        {
            _logger.LogInformation("GetEventOffer triggered for SpendX");

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
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "OfferId or valid PageId and PagePosition is required." });
                }

                var result = await _spendXService.GetEventOfferAsync(request);
                if (result.Error != null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in GetEventOffer: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while retrieving the event offer." });
            }
        }

        [Function("SaveEventOfferOverview")]
        [OpenApiOperation(operationId: "SaveEventOfferOverview", tags: new[] { "EventOffer" }, Summary = "Save SpendX offer overview", Description = "Updates SpendX offer tiers, overview, pricing, and merchandising flags.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SpendXOverviewRequestDto), Required = true, Description = "SpendX overview update payload")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SpendXOverviewSaveResponseDto), Description = "Overview saved successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> SaveEventOfferOverview(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/SaveEventOfferOverview")] HttpRequestData req)
        {
            _logger.LogInformation("SaveEventOfferOverviewAsync triggered for SpendX");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<SpendXOverviewRequestDto>(body);

                if (request == null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                }

                var valResult = await _overviewValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.SaveEventOfferOverviewAsync(request);
                if (result.Error != null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SaveEventOfferOverviewAsync: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while saving the event offer overview." });
            }
        }

        [Function("EventOfferDataView")]
        [OpenApiOperation(operationId: "EventOfferDataView", tags: new[] { "EventOffer" }, Summary = "Get SpendX offer data view", Description = "Retrieves paginated SKU-level pricing, forecasts, and tier details for an offer.")]
        [OpenApiParameter(name: "EventId", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Event ID")]
        [OpenApiParameter(name: "EventOfferId", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Offer ID")]
        [OpenApiParameter(name: "OfferNo", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Offer Number (default 1)")]
        [OpenApiParameter(name: "PageNumber", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Pagination page number (default 1)")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Pagination page size (default 100)")]
        [OpenApiParameter(name: "Spend1", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 1 spend threshold")]
        [OpenApiParameter(name: "SavePct1", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 1 save percentage")]
        [OpenApiParameter(name: "Spend2", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 2 spend threshold")]
        [OpenApiParameter(name: "SavePct2", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 2 save percentage")]
        [OpenApiParameter(name: "Spend3", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 3 spend threshold")]
        [OpenApiParameter(name: "SavePct3", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Tier 3 save percentage")]
        [OpenApiParameter(name: "IncrementalPercent", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Incremental forecast percentage")]
        [OpenApiParameter(name: "SpacePurchase", In = ParameterLocation.Query, Required = false, Type = typeof(decimal), Description = "Space purchase amount")]
        [OpenApiParameter(name: "SKUs", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Comma-separated SKUs filter")]
        [OpenApiParameter(name: "Brands", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Comma-separated brands filter")]
        [OpenApiParameter(name: "isOfferEdited", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Trigger recalculation")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DataViewResponseDto), Description = "Paginated DataView response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> EventOfferDataView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EventOffer/EventOfferDataView")] HttpRequestData req)
        {
            _logger.LogInformation("EventOfferDataView triggered for SpendX");

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
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.GetDataViewAsync(request);
                if (result.Error != null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in EventOfferDataView: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while retrieving data view." });
            }
        }

        [Function("PostEventOfferDataView")]
        [OpenApiOperation(operationId: "PostEventOfferDataView", tags: new[] { "EventOffer" }, Summary = "Save SpendX offer data view", Description = "Updates SKU-level forecasts and scan support values and recalculates header rollups.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(DataViewResponseDto), Required = true, Description = "DataView SKU updates payload")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DataviewCalculationsResponseDto), Description = "Updated header financial calculations")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> PostEventOfferDataView(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/PostEventOfferDataView")] HttpRequestData req)
        {
            _logger.LogInformation("PostEventOfferDataView triggered for SpendX");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<DataViewResponseDto>(body);

                if (request == null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Payload cannot be null." });
                }

                var valResult = await _dataViewResponseValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.PostDataViewAsync(request);
                if (result.Error != null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in PostEventOfferDataView: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while saving data view." });
            }
        }

        [Function("MoveOffer")]
        [OpenApiOperation(operationId: "MoveOffer", tags: new[] { "EventOffer" }, Summary = "Move SpendX offer position", Description = "Moves an offer from a source page/position to a target page/position atomically.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MoveOfferRequestDto), Required = true, Description = "MoveOffer request payload")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MoveOfferResponseDto), Description = "Offer moved successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> MoveOffer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventOffer/MoveOffer")] HttpRequestData req)
        {
            _logger.LogInformation("MoveOffer triggered for SpendX");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<MoveOfferRequestDto>(body);

                if (request == null)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BAD_REQUEST", Message = "Request payload cannot be null." });
                }

                var valResult = await _moveOfferValidator.ValidateAsync(request);
                if (!valResult.IsValid)
                {
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.MoveOfferAsync(request);
                if (result.Error != null)
                {
                    var statusCode = result.Error.ErrorCode switch
                    {
                        "NOT_FOUND" => HttpStatusCode.NotFound,
                        "BAD_REQUEST" or "VALIDATION_FAILED" => HttpStatusCode.BadRequest,
                        _ => HttpStatusCode.InternalServerError
                    };
                    return await CreateJsonResponseAsync(req, statusCode, result);
                }

                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in MoveOffer: {Message}", ex.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new ErrorDto { ErrorCode = "INTERNAL_ERROR", Message = "An unexpected error occurred while moving offer." });
            }
        }

        [Function("GetPricingExtract")]
        [OpenApiOperation(operationId: "GetPricingExtract", tags: new[] { "EventOffer" }, Summary = "Get SpendX pricing extract", Description = "Generates the SpendX pricing extract partitioned into standard and loyal sheets.")]
        [OpenApiParameter(name: "eventId", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Event ID")]
        [OpenApiParameter(name: "UserId", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "User ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PricingExtractResponseDto), Description = "Pricing extract generated successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Validation failed")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorDto), Description = "Internal server error")]
        public async Task<HttpResponseData> GetPricingExtract(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "OutBound/GetPricingExtract")] HttpRequestData req)
        {
            _logger.LogInformation("GetPricingExtract triggered");

            try
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var request = new PricingExtractRequestDto
                {
                    EventId = int.TryParse(queryParams["eventId"], out var id) ? id : 0,
                    UserId = queryParams["UserId"]
                };
                _logger.LogInformation("GetPricingExtract received: EventId={EventId}, UserId={UserId}", request.EventId, request.UserId);

                var validationResult = await _pricingExtractValidator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation failed for GetPricingExtract: EventId={EventId}, Errors={Errors}", request.EventId, string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto
                    {
                        ErrorCode = "VALIDATION_FAILED",
                        Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _spendXService.GetPricingExtractAsync(request.EventId, request.UserId);

                if (result.Error != null)
                {
                    _logger.LogWarning("GetPricingExtract service error: EventId={EventId}, ErrorCode={ErrorCode}, Message={Message}",
                        request.EventId, result.Error.ErrorCode, result.Error.Message);
                    return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, result);
                }

                _logger.LogInformation("GetPricingExtract completed: EventId={EventId}", request.EventId);
                return await CreateJsonResponseAsync(req, HttpStatusCode.OK, result);
            }
            catch (FluentValidation.ValidationException vex)
            {
                _logger.LogWarning(vex, "Validation failed in GetPricingExtract");
                return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new ErrorDto { ErrorCode = "BadRequest", Message = "Validation failed.", Detail = string.Join("; ", vex.Errors.Select(e => e.ErrorMessage)), Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                var error = new ErrorDto
                {
                    ErrorCode = "OUTBOUND_PRICING_EXTRACT_ERROR",
                    Message = "An unexpected error occurred while retrieving the pricing extract.",
                    Timestamp = DateTime.Now
                };

                _logger.LogError(ex, "GetPricingExtract failed. ExceptionType: {ExceptionType}, InnerException: {InnerMessage}", ex.GetType().Name, ex.InnerException?.Message);
                return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, error);
            }
        }

        private static async Task<HttpResponseData> CreateJsonResponseAsync<T>(HttpRequestData req, HttpStatusCode statusCode, T payload)
        {
            var response = req.CreateResponse();
            response.StatusCode = statusCode;
            await response.WriteAsJsonAsync(payload);
            return response;
        }
    }
}
