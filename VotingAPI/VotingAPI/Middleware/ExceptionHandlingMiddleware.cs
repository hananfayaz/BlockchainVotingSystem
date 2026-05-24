using System.Text.Json;

namespace VotingAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                context.Response.StatusCode = ex switch
                {
                    ArgumentException => 400,
                    UnauthorizedAccessException => 401,
                    KeyNotFoundException => 404,
                    InvalidOperationException => 409,
                    _ => 500,
                };

                var response = new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response)
                );
            }
        }
    }
}