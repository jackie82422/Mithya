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

// Database
builder.Services.AddDbContext<MockServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IEndpointRepository, EndpointRepository>();
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();

// Protocol Handler Factory
builder.Services.AddSingleton<ProtocolHandlerFactory>();

// WireMock Server Manager (Singleton)
builder.Services.AddSingleton<WireMockServerManager>(sp =>
{
    var scope = sp.CreateScope();
    var endpointRepo = scope.ServiceProvider.GetRequiredService<IEndpointRepository>();
    var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
    var factory = sp.GetRequiredService<ProtocolHandlerFactory>();

    var port = builder.Configuration.GetValue<int>("WireMock:Port", 5001);
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

// Initialize WireMock server
var wireMockManager = app.Services.GetRequiredService<WireMockServerManager>();
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

app.Run();
