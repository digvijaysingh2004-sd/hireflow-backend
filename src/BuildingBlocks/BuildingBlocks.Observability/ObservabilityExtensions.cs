using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddHireFlowObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        // Read Honeycomb / OTLP settings
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] 
            ?? configuration["Honeycomb:Endpoint"] 
            ?? "https://api.honeycomb.io";

        var otlpHeaders = configuration["OTEL_EXPORTER_OTLP_HEADERS"] 
            ?? (configuration["Honeycomb:ApiKey"] != null ? $"x-honeycomb-team={configuration["Honeycomb:ApiKey"]}" : null)
            ?? (configuration["Honeycomb:KeySecret"] != null ? $"x-honeycomb-team={configuration["Honeycomb:KeySecret"]}" : null);

        var hasOtlpExporter = !string.IsNullOrWhiteSpace(otlpHeaders);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.Filter = ctx => 
                            !ctx.Request.Path.StartsWithSegments("/health") &&
                            !ctx.Request.Path.StartsWithSegments("/swagger");
                    })
                    .AddHttpClientInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                    });

                if (hasOtlpExporter)
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otlpEndpoint);
                        otlpOptions.Headers = otlpHeaders;
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (hasOtlpExporter)
                {
                    metrics.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(otlpEndpoint);
                        otlpOptions.Headers = otlpHeaders;
                        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            });

        return services;
    }
}
