using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Application.ProductManuals;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/internal/product-manual-security")]
[AllowAnonymous]
public sealed class ProductManualSecurityEventsController : ControllerBase
{
    private const string SubscriptionValidationEventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
    private const string MalwareScanningResultEventType = "Microsoft.Security.MalwareScanningResult";
    private const string WebhookSecretConfigurationKey = "ProductManualSecurity:EventGridWebhookSecret";

    private readonly IProductManualIndexService _productManualIndexService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductManualSecurityEventsController> _logger;

    public ProductManualSecurityEventsController(IProductManualIndexService productManualIndexService, IConfiguration configuration, ILogger<ProductManualSecurityEventsController> logger)
    {
        _productManualIndexService = productManualIndexService ?? throw new ArgumentNullException(nameof(productManualIndexService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("events")]
    public async Task<IActionResult> Receive([FromQuery(Name = "token")] string? token, [FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        if (!IsAuthorized(token))
        {
            _logger.LogWarning("Rejected an unauthorized ProductCare malware-scan webhook request.");
            return Unauthorized();
        }

        if (payload.ValueKind != JsonValueKind.Array)
        {
            return BadRequest();
        }

        foreach (JsonElement eventEnvelope in payload.EnumerateArray())
        {
            string? eventType = GetString(eventEnvelope, "eventType");

            if (!string.Equals(eventType, SubscriptionValidationEventType, StringComparison.Ordinal))
            {
                continue;
            }

            if (!eventEnvelope.TryGetProperty("data", out JsonElement data))
            {
                return BadRequest();
            }

            string? validationCode = GetString(data, "validationCode");

            if (string.IsNullOrWhiteSpace(validationCode))
            {
                return BadRequest();
            }

            _logger.LogInformation("Completed Azure Event Grid webhook endpoint validation for ProductCare manual security events.");
            return Ok(new { validationResponse = validationCode });
        }

        foreach (JsonElement eventEnvelope in payload.EnumerateArray())
        {
            string? eventType = GetString(eventEnvelope, "eventType");

            if (!string.Equals(eventType, MalwareScanningResultEventType, StringComparison.Ordinal))
            {
                continue;
            }

            if (!eventEnvelope.TryGetProperty("data", out JsonElement data))
            {
                _logger.LogWarning("Ignored a malware scan event that did not contain a data object.");
                continue;
            }

            string? blobUriText = GetString(data, "blobUri");
            string? blobETag = GetString(data, "eTag");
            string? scanResultType = GetString(data, "scanResultType");
            string? eventId = GetString(eventEnvelope, "id");

            if (!Uri.TryCreate(blobUriText, UriKind.Absolute, out Uri? blobUri) || string.IsNullOrWhiteSpace(blobETag) || string.IsNullOrWhiteSpace(scanResultType))
            {
                _logger.LogWarning("Ignored malformed Defender for Storage malware scan event {EventId}.", eventId);
                continue;
            }

            try
            {
                await _productManualIndexService.ProcessSecurityScanResultAsync(blobUri, blobETag, scanResultType, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "ProductCare could not complete malware scan event {EventId}. Returning a retryable response to Azure Event Grid.", eventId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        return Ok();
    }

    private bool IsAuthorized(string? suppliedToken)
    {
        string? expectedToken = _configuration[WebhookSecretConfigurationKey];

        if (string.IsNullOrWhiteSpace(expectedToken) || expectedToken.Length < 32 || string.IsNullOrWhiteSpace(suppliedToken))
        {
            return false;
        }

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);

        return expectedBytes.Length == suppliedBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
