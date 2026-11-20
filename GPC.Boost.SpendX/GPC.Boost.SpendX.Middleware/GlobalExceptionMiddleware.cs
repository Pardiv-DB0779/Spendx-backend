using System;
using System.Net;
using System.Threading.Tasks;
using GPC.Boost.SpendX.Dto.ResponseDto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace GPC.Boost.SpendX.Middleware
{
    
    public class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
    {
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
                _logger.LogError(ex, "Unhandled exception in function execution context: {Message}", ex.Message);
                try
                {
                    var req = await context.GetHttpRequestDataAsync();
                    if (req != null)
                    {
                        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                        await response.WriteAsJsonAsync(new APIResponseDto
                        {
                            Error = new ErrorDto
                            {
                                ErrorCode = "INTERNAL_SERVER_ERROR",
                                Message = "An unexpected error occurred while processing the request.",
                                Detail = ex.Message
                            }
                        });
                        context.GetInvocationResult().Value = response;
                        return;
                    }
                }
                catch
                {
                    // If HTTP context cannot be resolved (non-HTTP trigger or context feature unavailable), rethrow original exception
                }

                throw;
            }
        }
    }
}
