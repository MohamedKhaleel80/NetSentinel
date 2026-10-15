using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetSentinel.API.Data;
using NetSentinel.API.Hubs;
using NetSentinel.API.Models.Identity;
using NetSentinel.API.Models.Settings;
using NetSentinel.API.Services;
using NetSentinel.API.Services.Interfaces;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//
// Database
//

builder.Services.AddDbContext<NetSentinelDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString("DefaultConnection")));

//
// Settings
//

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<FirebaseSettings>(
    builder.Configuration.GetSection("FirebaseSettings"));

builder.Services.Configure<SnortSettings>(
    builder.Configuration.GetSection("SnortSettings"));

var jwtSettings =
    builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "JwtSettings configuration is missing.");

if (string.IsNullOrWhiteSpace(
    jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is missing. Configure JwtSettings__SecretKey through environment-specific secret storage.");
}

var snortSettings =
    builder.Configuration
        .GetSection("SnortSettings")
        .Get<SnortSettings>()
    ?? throw new InvalidOperationException(
        "SnortSettings configuration is missing.");

if (string.IsNullOrWhiteSpace(snortSettings.ApiKey))
{
    throw new InvalidOperationException(
        "SnortSettings:ApiKey is missing. Configure SnortSettings__ApiKey through environment-specific secret storage.");
}

var corsAllowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? Array.Empty<string>();

if (!builder.Environment.IsDevelopment())
{
    if (corsAllowedOrigins.Length == 0 ||
        corsAllowedOrigins.Any(origin =>
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo)))
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must contain one or more exact HTTP or HTTPS origins without paths, queries, or wildcards.");
    }
}

//
// Identity
//

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(
        options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);

            options.Lockout.MaxFailedAccessAttempts = 5;

            options.Lockout.AllowedForNewUsers = true;
        })
    .AddEntityFrameworkStores<NetSentinelDbContext>()
    .AddDefaultTokenProviders();

//
// JWT
//

var signingKey =
    new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            jwtSettings.SecretKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer =
                    jwtSettings.Issuer,

                ValidateAudience = true,

                ValidAudience =
                    jwtSettings.Audience,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    signingKey,

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromMinutes(1)
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/alerts"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

//
// Authorization
//

builder.Services.AddAuthorization();

//
// CORS
//

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "NetSentinelMobile",
        policy =>
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

//
// Rate Limiting
//

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter(
        "api",
        limiterOptions =>
        {
            limiterOptions.PermitLimit = 100;
            limiterOptions.Window =
                TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });
});

//
// SignalR
//

builder.Services.AddSignalR();

//
// Application Services
//

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ISnortService, SnortService>();

builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAlertNotificationService, AlertNotificationService>();

builder.Services.AddScoped<IIncidentService, IncidentService>();

builder.Services.AddSingleton<IFirewallService, FirewallService>();
builder.Services.AddScoped<IBlockIpService, BlockIpService>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<IFcmNotificationService, FcmNotificationService>();

builder.Services.AddScoped<
    IDeviceDiscoveryService,
    DeviceDiscoveryService>();

//
// Background Services
//

builder.Services.AddHostedService<SnortAlertBackgroundService>();

//
// Controllers / OpenAPI
//

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

//
// Http Context
//

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

//
// Database Roles
//

using (var scope =
    app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(
        scope.ServiceProvider);
}

//
// Development OpenAPI
//

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "NetSentinel API v1");
    });
}

//
// Middleware
//

app.UseCors("NetSentinelMobile");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

//
// Endpoints
//

app.MapControllers()
    .RequireRateLimiting("api");

app.MapHub<AlertHub>(
    "/hubs/alerts");

app.Run();