namespace MindCare.Middleware;

/// <summary>
/// Prevents browsers from retaining authenticated pages in their history cache.
/// Authentication itself remains controlled by the Identity cookie and logout action.
/// </summary>
public sealed class AuthenticatedResponseNoCacheMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedResponseNoCacheMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}
