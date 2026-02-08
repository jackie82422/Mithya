using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.Repositories;
using MockServer.Infrastructure.ProtocolHandlers;
using MockServer.Infrastructure.MockEngine;
using MockServer.Api.Endpoints;
using MockServer.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Database
builder.Services.AddDbContext<MockServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IEndpointRepository, EndpointRepository>();
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<IProxyConfigRepository, ProxyConfigRepository>();

// Protocol Handler Factory
builder.Services.AddSingleton<ProtocolHandlerFactory>();

// Mock Engine
builder.Services.AddSingleton<ITemplateEngine, HandlebarsTemplateEngine>();
builder.Services.AddSingleton<IMockRuleCache, MockRuleCache>();
builder.Services.AddSingleton<IMatchEngine, MatchEngine>();
builder.Services.AddSingleton<IFaultInjector, FaultInjector>();
builder.Services.AddSingleton<ResponseRenderer>();

// Proxy Engine
builder.Services.AddHttpClient("ProxyClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });
builder.Services.AddSingleton<IProxyEngine, ProxyEngine>();
builder.Services.AddSingleton<IRecordingService, RecordingService>();
builder.Services.AddSingleton<IProxyConfigCache, ProxyConfigCache>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate database (skip for InMemory database in tests)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MockServerDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Dynamic mock middleware (must be before MapXxxApis so non-admin paths are intercepted)
app.UseMiddleware<DynamicMockMiddleware>();

// Map API endpoints
app.MapProtocolEndpoints();
app.MapEndpointManagementApis();
app.MapRuleManagementApis();
app.MapLogApis();
app.MapTemplateApis();
app.MapProxyConfigApis();
app.MapConfigEndpoints();

// Load all cached rules on startup
var cache = app.Services.GetRequiredService<IMockRuleCache>();
try
{
    await cache.LoadAllAsync();
    Console.WriteLine("Mock rule cache loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading mock rule cache: {ex.Message}");
}

// Load proxy config cache on startup
var proxyCache = app.Services.GetRequiredService<IProxyConfigCache>();
try
{
    await proxyCache.LoadAllAsync();
    Console.WriteLine("Proxy config cache loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading proxy config cache: {ex.Message}");
}

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
