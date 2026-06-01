using EasyDisk.API.Exceptions;
using EasyDisk.Application.Exceptions;
using EasyDisk.Domain.Exceptions;
using EasyDisk.Infrastructure.Exceptions;
using Google.Apis.Auth;
using Stripe;
using System.Net;
using System.Text.Json;

namespace EasyDisk.API.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                response.StatusCode = error switch
                {
                    ValidationException => (int)HttpStatusCode.BadRequest,
                    StorageOperationException => (int)HttpStatusCode.BadRequest,
                    InvalidJwtException => (int)HttpStatusCode.BadRequest,
                    StripeException => (int)HttpStatusCode.BadRequest,
                    NotFoundException => (int)HttpStatusCode.NotFound,
                    DomainException => (int)HttpStatusCode.UnprocessableEntity,
                    InfrastructureException => (int)HttpStatusCode.ServiceUnavailable,
                    ApiException => (int)HttpStatusCode.InternalServerError,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                if (response.StatusCode == (int)HttpStatusCode.InternalServerError || error is StripeException || error is InvalidJwtException)
                {
                    _logger.LogError(error, "Unexpected server error: {Message}", error.Message);
                }

                var errorMessage = response.StatusCode == (int)HttpStatusCode.InternalServerError
                    ? "Internal server error. Please try again later."
                    : error.Message;

                if (error is InvalidJwtException) errorMessage = "Invalid Google token.";
                if (error is StripeException) errorMessage = "Error validating the request from the payment system.";

                var result = JsonSerializer.Serialize(new { message = errorMessage });
                await response.WriteAsync(result);
            }
        }
    }
}
