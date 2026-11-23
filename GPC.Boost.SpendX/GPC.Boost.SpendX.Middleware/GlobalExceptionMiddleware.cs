using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using GPC.Boost.SpendX.Dto.ResponseDto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace GPC.Boost.SpendX.Middleware
{
    public class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
    {
        private const string HttpTriggerType = "httpTrigger";
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                // Unwrap any TargetInvocationException / AggregateException to get the underlying root cause
                var actualEx = ex.GetBaseException() ?? ex;

                var statusCode = HttpStatusCode.InternalServerError;
                var errorDto = new ErrorDto
                {
                    Type = actualEx.GetType().Name,
                    Title = actualEx.Source ?? "SpendX.Api",
                    Timestamp = DateTime.UtcNow
                };

                // 1. FluentValidation Exception
                if (actualEx is ValidationException valEx)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    errorDto.ErrorCode = "VALIDATION_FAILED";
                    errorDto.Message = "Validation failed.";
                    errorDto.Detail = string.Join("; ", valEx.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning(valEx, "Validation failed: {Message}, Status: {StatusCode}", errorDto.Detail, (int)statusCode);
                }
                // 2. Client / Argument Exceptions
                else if (actualEx is ArgumentException or FormatException)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    errorDto.ErrorCode = "BAD_REQUEST";
                    errorDto.Message = actualEx.Message;
                    errorDto.Detail = actualEx.Message;
                    _logger.LogWarning(actualEx, "Client request error: {Message}, Status: {StatusCode}", actualEx.Message, (int)statusCode);
                }
                // 3. Resource Not Found
                else if (actualEx is KeyNotFoundException)
                {
                    statusCode = HttpStatusCode.NotFound;
                    errorDto.ErrorCode = "NOT_FOUND";
                    errorDto.Message = actualEx.Message;
                    errorDto.Detail = "The requested resource could not be found.";
                    _logger.LogWarning(actualEx, "Resource not found: {Message}, Status: {StatusCode}", actualEx.Message, (int)statusCode);
                }
                // 4. Invalid Operation (Business rule violation)
                else if (actualEx is InvalidOperationException)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    errorDto.ErrorCode = "INVALID_OPERATION";
                    errorDto.Message = actualEx.Message;
                    errorDto.Detail = actualEx.Message;
                    _logger.LogWarning(actualEx, "Invalid operation: {Message}, Status: {StatusCode}", actualEx.Message, (int)statusCode);
                }
                // 5. Database Exceptions
                else if (actualEx.GetType().Name.Contains("DbUpdate") || actualEx.GetType().Name.Contains("Npgsql"))
                {
                    statusCode = HttpStatusCode.InternalServerError;
                    errorDto.ErrorCode = "DATABASE_ERROR";
                    errorDto.Message = "A database error occurred while processing the request.";
                    errorDto.Detail = actualEx.Message;
                    errorDto.InnerDetail = actualEx.InnerException?.Message;
                    _logger.LogError(actualEx, "Database error: {Message}, InnerException: {Inner}, StackTrace: {StackTrace}",
                        actualEx.Message, actualEx.InnerException?.Message, actualEx.StackTrace);
                }
                // 6. Unhandled Server Exception
                else
                {
                    statusCode = HttpStatusCode.InternalServerError;
                    errorDto.ErrorCode = "INTERNAL_SERVER_ERROR";
                    errorDto.Message = "An unexpected error occurred while processing the request.";
                    errorDto.Detail = actualEx.Message;
                    errorDto.InnerDetail = actualEx.InnerException?.Message;
                    _logger.LogError(actualEx, "Unhandled server error: {Message}, InnerException: {Inner}, StackTrace: {StackTrace}", 
                        actualEx.Message, actualEx.InnerException?.Message, actualEx.StackTrace);
                }

                errorDto.Status = (int)statusCode;

                // Check if the current function is triggered by an HTTP trigger
                var isHttpTrigger = context.FunctionDefinition.InputBindings.Values
                    .Any(b => b.Type.Equals(HttpTriggerType, StringComparison.OrdinalIgnoreCase));

                if (isHttpTrigger)
                {
                    var req = await context.GetHttpRequestDataAsync();
                    if (req != null)
                    {
                        errorDto.Instance = req.Url.AbsolutePath;
                        var response = req.CreateResponse(statusCode);
                        await response.WriteAsJsonAsync(errorDto);
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                }

                // For non-HTTP triggers or if HTTP context is unavailable, rethrow
                throw;
            }
        }
    }
}
