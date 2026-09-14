using MindCare.Models;

namespace MindCare.Middleware;

public sealed class CounsellorPasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public CounsellorPasswordChangeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var requiresChange = user.Identity?.IsAuthenticated == true &&
            user.IsInRole(RoleNames.Counsellor) &&
            user.HasClaim(SecurityClaimTypes.MustChangePassword, SecurityClaimTypes.True);

        if (requiresChange && !IsAllowed(context.Request.Path))
        {
            context.Response.Redirect("/Counsellor/ChangeInitialPassword");
            return;
        }

        await _next(context);
    }

    private static bool IsAllowed(PathString path) =>
        path.StartsWithSegments("/Counsellor/ChangeInitialPassword") ||
        path.StartsWithSegments("/Account/Logout") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/images") ||
        path.StartsWithSegments("/favicon.ico");
}
