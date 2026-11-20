using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using GPC.Boost.SpendX.Dto.RequestDto;
using GPC.Boost.SpendX.Dto.ResponseDto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GPC.Boost.SpendX.Middleware
{
   
    public class ValidationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<ValidationMiddleware> _logger;

        public ValidationMiddleware(ILogger<ValidationMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var requestData = await context.GetHttpRequestDataAsync();
            if (requestData != null && requestData.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var functionName = context.FunctionDefinition.Name;

                if (functionName.Equals("AddEventOffer", StringComparison.OrdinalIgnoreCase))
                {
                    var validator = context.InstanceServices.GetService<IValidator<EventOfferRequestDto>>();
                    if (validator != null)
                    {
                        requestData.Body.Position = 0;
                        using var reader = new StreamReader(requestData.Body, leaveOpen: true);
                        var body = await reader.ReadToEndAsync();
                        requestData.Body.Position = 0;

                        if (string.IsNullOrWhiteSpace(body))
                        {
                            await ShortCircuitWithValidationError(context, requestData, "Request payload cannot be empty.");
                            return;
                        }

                        var dto = JsonConvert.DeserializeObject<EventOfferRequestDto>(body);
                        if (dto == null)
                        {
                            await ShortCircuitWithValidationError(context, requestData, "Malformed or invalid JSON payload.");
                            return;
                        }

                        var valResult = await validator.ValidateAsync(dto);
                        if (!valResult.IsValid)
                        {
                            var errors = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage));
                            _logger.LogWarning("Validation failed for {FunctionName}: {Errors}", functionName, errors);
                            await ShortCircuitWithValidationError(context, requestData, errors);
                            return;
                        }
                    }
                }
                else if (functionName.Equals("SaveEventOfferOverview", StringComparison.OrdinalIgnoreCase))
                {
                    var validator = context.InstanceServices.GetService<IValidator<SpendXOverviewRequestDto>>();
                    if (validator != null)
                    {
                        requestData.Body.Position = 0;
                        using var reader = new StreamReader(requestData.Body, leaveOpen: true);
                        var body = await reader.ReadToEndAsync();
                        requestData.Body.Position = 0;

                        if (string.IsNullOrWhiteSpace(body))
                        {
                            await ShortCircuitWithValidationError(context, requestData, "Request payload cannot be empty.");
                            return;
                        }

                        var dto = JsonConvert.DeserializeObject<SpendXOverviewRequestDto>(body);
                        if (dto == null)
                        {
                            await ShortCircuitWithValidationError(context, requestData, "Malformed or invalid JSON payload.");
                            return;
                        }

                        var valResult = await validator.ValidateAsync(dto);
                        if (!valResult.IsValid)
                        {
                            var errors = string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage));
                            _logger.LogWarning("Validation failed for {FunctionName}: {Errors}", functionName, errors);
                            await ShortCircuitWithValidationError(context, requestData, errors);
                            return;
                        }
                    }
                }
            }

            await next(context);
        }

        private static async Task ShortCircuitWithValidationError(FunctionContext context, HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new APIResponseDto
            {
                Error = new ErrorDto
                {
                    ErrorCode = "VALIDATION_FAILED",
                    Message = message
                }
            });
            context.GetInvocationResult().Value = response;
        }
    }
}
