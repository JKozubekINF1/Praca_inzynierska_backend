using System.Net;
using System.Text.Json;

namespace Market.Middleware
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

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                string message = error.Message;

                switch (error)
                {
                    case KeyNotFoundException:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;

                    case UnauthorizedAccessException:
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        break;

                    case ArgumentException:
                    case InvalidOperationException:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;

                    default:
                        _logger.LogError(error, "Nieoczekiwany błąd");
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        message = "Wystąpił wewnętrzny błąd serwera.";
                        break;
                }

                var result = JsonSerializer.Serialize(new { message = message });
                await response.WriteAsync(result);
            }
        }
    }
}