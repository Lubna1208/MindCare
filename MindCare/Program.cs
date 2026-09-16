using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MindCare.Data;
using MindCare.Models;
using MindCare.Services;
using MindCare.Services.AI;
using MindCare.Middleware;
using Stripe;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure());
    options.ConfigureWarnings(warnings =>
        warnings.Log(RelationalEventId.PendingModelChangesWarning));
});

// Development sessions should not survive an application restart. The ephemeral
// provider creates fresh in-memory keys for each process, so prior cookie tickets
// can no longer be decrypted after the next development launch.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .UseEphemeralDataProtectionProvider();
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<RoleRedirectService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAppointmentChatWindowService, AppointmentChatWindowService>();
builder.Services.AddSingleton<IVideoCallRoomService, VideoCallRoomService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
var aiProvider = builder.Configuration["AI:Provider"];
if (!string.Equals(aiProvider, "Gemini", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Configured AI provider is not supported.");
}

builder.Services.AddHttpClient<IAIService, GeminiAIService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(25);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai-faq", context =>
    {
        var partitionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.AddPolicy("ai-resource-summary", context =>
    {
        var partitionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
builder.Services.AddHostedService<AppointmentChatNotificationWorker>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);
await ResourceSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<AuthenticatedResponseNoCacheMiddleware>();
app.UseMiddleware<CounsellorPasswordChangeMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
