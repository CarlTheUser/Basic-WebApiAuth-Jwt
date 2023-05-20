using Application;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Web
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
        {
            httpContext.Response.ContentType = "application/problem+json";

            if (exception is TaskCanceledException)
            {
                return;
            }

            _logger.LogError(exception, "Fatal Error Occurred");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(
                new ProblemDetails()
                {
                    Type = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/errors/internal-server-error",
                    Title = "An internal server error occurred.",
                    Detail = "Something happened in the server and we couldn't process your request.",
                    Instance = httpContext.Request.Path,
                    Status = httpContext.Response.StatusCode
                }));
        }
    }
}