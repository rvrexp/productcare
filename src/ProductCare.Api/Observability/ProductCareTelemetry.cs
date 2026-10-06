using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ProductCare.Api.Observability;

public static class ProductCareTelemetry
{
    public const string ActivitySourceName = "ProductCare.ProductAssistant";
    public const string MeterName = "ProductCare.ProductAssistant";
    public const string AgentId = "productcare-product-assistant-v1";
    public const string AgentName = "ProductCare Product Assistant";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> AssistantRequests = Meter.CreateCounter<long>("productcare.assistant.requests");
    public static readonly Counter<long> AssistantFailures = Meter.CreateCounter<long>("productcare.assistant.failures");
    public static readonly Counter<long> ToolCalls = Meter.CreateCounter<long>("productcare.assistant.tool.calls");
    public static readonly Histogram<double> AssistantDuration = Meter.CreateHistogram<double>("productcare.assistant.duration", "ms");
}