using System.Text;
using Epsilon.Core.LLM;
using Epsilon.Web.Auth;
using Epsilon.Web.Data;
using Epsilon.Web.Endpoints;
using Epsilon.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// JSON serialization — camelCase for frontend compatibility
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Database — prefer env var (Cloud Run secrets), fall back to config
var connStr = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr));

// Auth
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<UserContext>();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? "epsilon-dev-secret-key-min-32-chars!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Prevent default claim type mapping so "sub" stays as "sub"
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Epsilon",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EpsilonApp",
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization();

// HttpClient
builder.Services.AddSingleton<HttpClient>();

// LLM Providers (reuse from Core — all take HttpClient)
builder.Services.AddSingleton<ProviderRegistry>(sp =>
{
    var http = sp.GetRequiredService<HttpClient>();
    var registry = new ProviderRegistry();
    registry.Register(new Epsilon.Core.LLM.OpenAiProvider(http));
    registry.Register(new Epsilon.Core.LLM.AnthropicProvider(http));
    registry.Register(new Epsilon.Core.LLM.GeminiProvider(http));
    registry.Register(new Epsilon.Core.LLM.OllamaProvider(http));
    return registry;
});

// Web Services
builder.Services.AddScoped<UserKeyStore>();
builder.Services.AddScoped<WebDocumentRetriever>();
builder.Services.AddScoped<WebChatService>();
builder.Services.AddScoped<WebResearchService>();
builder.Services.AddScoped<WebSolverService>();
builder.Services.AddScoped<DocumentStorageService>();

// CORS — only needed in development (production serves frontend from same origin)
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
if (frontendUrl != "*")
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(frontendUrl)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
}

var app = builder.Build();

// Middleware
if (frontendUrl != "*")
    app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Extract UserContext from JWT claims
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        var userContext = ctx.RequestServices.GetRequiredService<UserContext>();
        var sub = ctx.User.FindFirst("sub")?.Value;
        if (sub != null && Guid.TryParse(sub, out var userId))
        {
            userContext.UserId = userId;
            userContext.Email = ctx.User.FindFirst("email")?.Value ?? "";
        }
    }
    await next();
});

// Map all API endpoints
app.MapAuthEndpoints();
app.MapChatEndpoints();
app.MapSolverEndpoints();
app.MapResearchEndpoints();
app.MapDocumentEndpoints();
app.MapFlashcardEndpoints();
app.MapSettingsEndpoints();

// Serve React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// Auto-create database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
