namespace BHG.WebService
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/game"))
            {
                if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Api key missing");
                    return;
                }

                if (apiKey != AppConfig.GetInstance().ApiKey)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid api key");
                    return;
                }
            }

            await _next(context);
        }
    }
}
