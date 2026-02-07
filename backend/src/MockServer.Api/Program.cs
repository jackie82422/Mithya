using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MockServer.Core.Interfaces;
using MockServer.Infrastructure.Data;
using MockServer.Infrastructure.Repositories;
using MockServer.Infrastructure.ProtocolHandlers;
using MockServer.Infrastructure.WireMock;
using MockServer.Api.Endpoints;

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

// Protocol Handler Factory
builder.Services.AddSingleton<ProtocolHandlerFactory>();

// WireMock Server Manager (Singleton) - won't start if port is 0 (test environment)
builder.Services.AddSingleton<WireMockServerManager>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var port = config.GetValue<int>("WireMock:Port", 5001);

    var scope = sp.CreateScope();
    var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
    var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
    var factory = sp.GetRequiredService<ProtocolHandlerFactory>();

    return new WireMockServerManager(endpointRepo, ruleRepo, factory, port);
});

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Map API endpoints
app.MapProtocolEndpoints();
app.MapEndpointManagementApis();
app.MapRuleManagementApis();
app.MapLogApis();

// Initialize WireMock server only if it's registered (skip in test environment)
var wireMockManager = app.Services.GetService<WireMockServerManager>();
if (wireMockManager is not null)
{
    wireMockManager.Start();

    // Sync rules from database
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            await wireMockManager.SyncAllRulesAsync();
            Console.WriteLine("Initial rule synchronization completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during initial sync: {ex.Message}");
        }
    }
}

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
