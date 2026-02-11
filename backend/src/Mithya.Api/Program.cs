using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.Repositories;
using Mithya.Infrastructure.ProtocolHandlers;
using Mithya.Infrastructure.MockEngine;
using Mithya.Api.Endpoints;
using Mithya.Api.Middleware;

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
builder.Services.AddDbContext<MithyaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IEndpointRepository, EndpointRepository>();
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<IProxyConfigRepository, ProxyConfigRepository>();
builder.Services.AddScoped<IServiceProxyRepository, ServiceProxyRepository>();
builder.Services.AddScoped<IScenarioRepository, ScenarioRepository>();
builder.Services.AddScoped<IScenarioStepRepository, ScenarioStepRepository>();
builder.Services.AddScoped<IEndpointGroupRepository, EndpointGroupRepository>();

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

// Try Request (admin proxy for testing)
builder.Services.AddHttpClient("TryRequest", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});
builder.Services.AddSingleton<IProxyEngine, ProxyEngine>();
builder.Services.AddSingleton<IRecordingService, RecordingService>();
builder.Services.AddSingleton<IProxyConfigCache, ProxyConfigCache>();
builder.Services.AddSingleton<IServiceProxyCache, ServiceProxyCache>();

// Scenario Engine
builder.Services.AddSingleton<IScenarioEngine, ScenarioEngine>();

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
    var db = scope.ServiceProvider.GetRequiredService<MithyaDbContext>();
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

// Global exception handler - must be outermost to catch all unhandled exceptions
app.UseMiddleware<GlobalExceptionHandler>();

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
app.MapServiceProxyApis();
app.MapScenarioApis();
app.MapEndpointGroupApis();
app.MapImportExportApis();
app.MapConfigEndpoints();
app.MapTryRequestApis();

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

// Load service proxy cache on startup
var serviceProxyCache = app.Services.GetRequiredService<IServiceProxyCache>();
try
{
    await serviceProxyCache.LoadAllAsync();
    Console.WriteLine("Service proxy cache loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading service proxy cache: {ex.Message}");
}

// Load scenario engine on startup
var scenarioEngine = app.Services.GetRequiredService<IScenarioEngine>();
try
{
    await scenarioEngine.LoadAllAsync();
    Console.WriteLine("Scenario engine loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading scenario engine: {ex.Message}");
}

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
