using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.ProductAssistant;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Api.Observability;
using ProductCare.Application.ProductAssistant;
using ProductCare.Application.Products;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;
using System.Diagnostics;
using System.Text.Json;

namespace ProductCare.Api.Controllers;

/// <summary>
/// Handles authenticated product-assistant requests for a specific saved product.
/// </summary>
[ApiController]
[Route("api/v1/products/{productId:guid}/assistant")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class ProductAssistantController : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProductService _productService;
    private readonly IProductAssistantService _productAssistantService;
    private readonly IUsageQuotaService _usageQuotaService;

    /// <summary>
    /// Creates the controller with product lookup and assistant orchestration dependencies.
    /// </summary>
    public ProductAssistantController(IProductService productService, IProductAssistantService productAssistantService, IUsageQuotaService usageQuotaService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _productAssistantService = productAssistantService ?? throw new ArgumentNullException(nameof(productAssistantService));
        _usageQuotaService = usageQuotaService ?? throw new ArgumentNullException(nameof(usageQuotaService));
    }

    /// <summary>
    /// Submits a conversational assistant request for the selected product and returns
    /// the answer with source and tool metadata.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductAssistantResponse>> Ask(Guid productId, AskProductAssistantRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productService.GetAsync(identity, productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(product.Manufacturer) || string.IsNullOrWhiteSpace(product.ModelNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product model information is required.",
                Detail = "A confirmed manufacturer and model number are required before ProductCare Assistant can answer product-specific questions."
            });
        }

        long started = Stopwatch.GetTimestamp();

        ProductCareTelemetry.AssistantRequests.Add(1);

        using Activity? activity = ProductCareTelemetry.ActivitySource.StartActivity("invoke_agent", ActivityKind.Internal);

        activity?.SetTag("gen_ai.operation.name", "invoke_agent");
        activity?.SetTag("gen_ai.agent.id", ProductCareTelemetry.AgentId);
        activity?.SetTag("gen_ai.agent.name", ProductCareTelemetry.AgentName);

        try
        {
            ProductAssistantQuery query = new(product.Id, product.Manufacturer, product.ModelNumber, product.ProductName, request.Message);

            await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductAssistant, cancellationToken);

            ProductAssistantResult result = await _productAssistantService.AskAsync(query, cancellationToken);

            RecordToolUsage(result, activity);

            activity?.SetStatus(ActivityStatusCode.Ok);

            ProductAssistantSourceResponse[] sources = result.Sources.Select(source => new ProductAssistantSourceResponse(source.Type.ToString(), source.Title, source.Url)).ToArray();

            return Ok(new ProductAssistantResponse(result.Answer, sources, result.ToolsUsed));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ProductCareTelemetry.AssistantFailures.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);

            throw;
        }
        finally
        {
            TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
            ProductCareTelemetry.AssistantDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Streams assistant answer text as newline-delimited JSON while preserving
    /// the same authentication, ownership, quota, and telemetry rules as the normal endpoint.
    /// </summary>
    [HttpPost("stream")]
    public async Task<IActionResult> Stream(Guid productId, AskProductAssistantRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productService.GetAsync(identity, productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(product.Manufacturer) || string.IsNullOrWhiteSpace(product.ModelNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product model information is required.",
                Detail = "A confirmed manufacturer and model number are required before ProductCare Assistant can answer product-specific questions."
            });
        }

        long started = Stopwatch.GetTimestamp();

        ProductCareTelemetry.AssistantRequests.Add(1);

        using Activity? activity = ProductCareTelemetry.ActivitySource.StartActivity("invoke_agent", ActivityKind.Internal);

        activity?.SetTag("gen_ai.operation.name", "invoke_agent");
        activity?.SetTag("gen_ai.agent.id", ProductCareTelemetry.AgentId);
        activity?.SetTag("gen_ai.agent.name", ProductCareTelemetry.AgentName);
        activity?.SetTag("productcare.streaming", true);

        try
        {
            ProductAssistantQuery query = new(product.Id, product.Manufacturer, product.ModelNumber, product.ProductName, request.Message);

            await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductAssistant, cancellationToken);

            Response.ContentType = "application/x-ndjson; charset=utf-8";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            await Response.StartAsync(cancellationToken);

            ProductAssistantResult result = await _productAssistantService.AskStreamingAsync(
                query,
                async (delta, token) =>
                {
                    ProductAssistantStreamResponse streamResponse = new("delta", delta, null);

                    await JsonSerializer.SerializeAsync(Response.Body, streamResponse, StreamJsonOptions, token);
                    await Response.WriteAsync("\n", token);
                    await Response.Body.FlushAsync(token);
                },
                cancellationToken);

            RecordToolUsage(result, activity);

            ProductAssistantSourceResponse[] sources = result.Sources.Select(source => new ProductAssistantSourceResponse(source.Type.ToString(), source.Title, source.Url)).ToArray();

            ProductAssistantResponse finalResponse = new(result.Answer, sources, result.ToolsUsed);
            ProductAssistantStreamResponse completedResponse = new("completed", null, finalResponse);

            await JsonSerializer.SerializeAsync(Response.Body, completedResponse, StreamJsonOptions, cancellationToken);
            await Response.WriteAsync("\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return new EmptyResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception exception)
        {
            ProductCareTelemetry.AssistantFailures.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);

            throw;
        }
        finally
        {
            TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
            ProductCareTelemetry.AssistantDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    private static void RecordToolUsage(ProductAssistantResult result, Activity? activity)
    {
        foreach (string tool in result.ToolsUsed)
        {
            ProductCareTelemetry.ToolCalls.Add(1, new KeyValuePair<string, object?>("gen_ai.tool.name", tool));
        }

        activity?.SetTag("productcare.tool.count", result.ToolsUsed.Count);
    }

    private sealed record ProductAssistantStreamResponse(string Type, string? Text, ProductAssistantResponse? Result);
}