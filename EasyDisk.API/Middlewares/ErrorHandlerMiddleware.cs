using EasyDisk.API.Exceptions;
using EasyDisk.Application.Exceptions;
using EasyDisk.Domain.Exceptions;
using EasyDisk.Infrastructure.Exceptions;
using System.Net;
using System.Text.Json;

namespace EasyDisk.API.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
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
                    NotFoundException => (int)HttpStatusCode.NotFound,
                    DomainException => (int)HttpStatusCode.UnprocessableEntity,
                    InfrastructureException => (int)HttpStatusCode.ServiceUnavailable,
                    ApiException => (int)HttpStatusCode.InternalServerError,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                var result = JsonSerializer.Serialize(new { message = error?.Message });
                await response.WriteAsync(result);
            }
        }
    }
}
