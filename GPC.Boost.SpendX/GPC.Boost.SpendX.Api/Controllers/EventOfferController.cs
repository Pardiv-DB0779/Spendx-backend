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
        private readonly ILogger<EventOfferController> _logger;

        public EventOfferController(
            ISpendXService spendXService,
            IValidator<EventOfferRequestDto> offerValidator,
            IValidator<SpendXOverviewRequestDto> overviewValidator,
            ILogger<EventOfferController> logger)
        {
            _spendXService = spendXService;
            _offerValidator = offerValidator;
            _overviewValidator = overviewValidator;
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
                    response.StatusCode = HttpStatusCode.InternalServerError;
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
                    response.StatusCode = HttpStatusCode.NotFound;
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
                    response.StatusCode = HttpStatusCode.InternalServerError;
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
    }
}
