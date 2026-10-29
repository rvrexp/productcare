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
    private readonly ILogger<ProductAssistantController> _logger;

    public ProductAssistantController(IProductService productService, IProductAssistantService productAssistantService, IUsageQuotaService usageQuotaService, ILogger<ProductAssistantController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _productAssistantService = productAssistantService ?? throw new ArgumentNullException(nameof(productAssistantService));
        _usageQuotaService = usageQuotaService ?? throw new ArgumentNullException(nameof(usageQuotaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductAssistant, cancellationToken);

            (string conversationId, bool hasConversationHistory) = await EnsureConversationAsync(identity, product, cancellationToken);

            ProductAssistantQuery query = new(product.Id, product.Manufacturer, product.ModelNumber, product.ProductName, request.Message)
            {
                AssistantConversationId = conversationId,
                HasConversationHistory = hasConversationHistory
            };

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
            await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductAssistant, cancellationToken);

            (string conversationId, bool hasConversationHistory) = await EnsureConversationAsync(identity, product, cancellationToken);

            ProductAssistantQuery query = new(product.Id, product.Manufacturer, product.ModelNumber, product.ProductName, request.Message)
            {
                AssistantConversationId = conversationId,
                HasConversationHistory = hasConversationHistory
            };

            bool streamingResponseStarted = false;

            async Task EnsureStreamingResponseStartedAsync(CancellationToken token)
            {
                if (streamingResponseStarted)
                {
                    return;
                }

                Response.ContentType = "application/x-ndjson; charset=utf-8";
                Response.Headers.CacheControl = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no";

                await Response.StartAsync(token);
                streamingResponseStarted = true;
            }

            ProductAssistantResult result = await _productAssistantService.AskStreamingAsync(
                query,
                async (delta, token) =>
                {
                    await EnsureStreamingResponseStartedAsync(token);

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

            await EnsureStreamingResponseStartedAsync(cancellationToken);

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

    [HttpPost("new-conversation")]
    public async Task<IActionResult> StartNewConversation(Guid productId, CancellationToken cancellationToken)
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

        string? previousConversationId = product.AssistantConversationId;

        await _productService.SetAssistantConversationIdAsync(identity, product.Id, null, cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousConversationId))
        {
            try
            {
                await _productAssistantService.DeleteConversationAsync(previousConversationId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "The previous ProductCare assistant conversation could not be deleted after it was disconnected from the product.");
            }
        }

        return NoContent();
    }

    private async Task<(string ConversationId, bool HasConversationHistory)> EnsureConversationAsync(UserIdentity identity, ProductRecord product, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(product.AssistantConversationId))
        {
            return (product.AssistantConversationId, true);
        }

        string conversationId = await _productAssistantService.CreateConversationAsync(cancellationToken);

        try
        {
            await _productService.SetAssistantConversationIdAsync(identity, product.Id, conversationId, cancellationToken);
        }
        catch
        {
            try
            {
                await _productAssistantService.DeleteConversationAsync(conversationId, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "A newly created ProductCare assistant conversation could not be cleaned up after persistence failed.");
            }

            throw;
        }

        return (conversationId, false);
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