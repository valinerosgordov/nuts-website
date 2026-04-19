namespace Nuts.Api.Options;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Leave empty to disable OpenTelemetry export.</summary>
    public string? OtlpEndpoint { get; init; }

    public string ServiceName { get; init; } = "nuts-api";
}
