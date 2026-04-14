using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds OpenTelemetry metrics, traces, and logging to the application.
    /// Sends all telemetry to the central OTel Collector via OTLP.
    /// </summary>
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName,
        string otlpEndpoint)
    {
        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddAttributes([
                new("deployment.environment", builder.Environment.EnvironmentName.ToLowerInvariant())
            ]);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resource);
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        });

        return builder;
    }
}
