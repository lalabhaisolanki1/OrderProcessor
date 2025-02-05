using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Enable Application Insights
builder.Services.AddApplicationInsightsTelemetry();

builder.ConfigureFunctionsWebApplication();
builder.Build().Run();
