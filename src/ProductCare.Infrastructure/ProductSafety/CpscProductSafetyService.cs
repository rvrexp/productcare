using Microsoft.Extensions.Logging;
using ProductCare.Application.ProductSafety;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductCare.Infrastructure.ProductSafety;

public sealed class CpscProductSafetyService : IProductSafetyService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CpscProductSafetyService> _logger;

    public CpscProductSafetyService(HttpClient httpClient, ILogger<CpscProductSafetyService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductSafetyCheckResult> CheckAsync(ProductSafetyQuery query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);

        string requestUri = $"Recall?format=json&RecallDescription={Uri.EscapeDataString(query.ModelNumber)}";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken);

            List<CpscRecallDto> recalls = await JsonSerializer.DeserializeAsync<List<CpscRecallDto>>(content, SerializerOptions, cancellationToken) ?? [];

            ProductRecall[] matchingRecalls = recalls
                .Where(recall => MatchesModelNumber(recall, query.ModelNumber))
                .Select(MapRecall)
                .OrderByDescending(recall => recall.RecallDate)
                .ToArray();

            _logger.LogInformation("CPSC recall check completed for a product model with {CandidateCount} candidates and {MatchCount} matching recalls.", recalls.Count, matchingRecalls.Length);

            return new ProductSafetyCheckResult(true, DateTime.UtcNow, matchingRecalls);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("CPSC recall request timed out.");
            return new ProductSafetyCheckResult(false, DateTime.UtcNow, []);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "CPSC recall request failed.");
            return new ProductSafetyCheckResult(false, DateTime.UtcNow, []);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "CPSC returned recall data that could not be parsed.");
            return new ProductSafetyCheckResult(false, DateTime.UtcNow, []);
        }
    }

    private static bool MatchesModelNumber(CpscRecallDto recall, string modelNumber)
    {
        string normalizedModel = Normalize(modelNumber);

        if (normalizedModel.Length < 3)
        {
            return false;
        }

        if (ContainsNormalized(recall.Description, normalizedModel))
        {
            return true;
        }

        return recall.Products?.Any(product =>
            string.Equals(Normalize(product.Model), normalizedModel, StringComparison.Ordinal) ||
            ContainsNormalized(product.Description, normalizedModel)) == true;
    }

    private static bool ContainsNormalized(string? value, string normalizedModel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Normalize(value).Contains(normalizedModel, StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private static ProductRecall MapRecall(CpscRecallDto recall)
    {
        string? hazard = JoinNames(recall.Hazards?.Select(item => item.Name));
        string? remedy = JoinNames(recall.Remedies?.Select(item => item.Name));

        return new ProductRecall(
            RecallNumber: string.IsNullOrWhiteSpace(recall.RecallNumber) ? "Unknown" : recall.RecallNumber,
            Title: string.IsNullOrWhiteSpace(recall.Title) ? "Product recall" : recall.Title,
            RecallDate: recall.RecallDate,
            Description: NormalizeOptionalText(recall.Description),
            Hazard: hazard,
            Remedy: remedy,
            ConsumerContact: NormalizeOptionalText(recall.ConsumerContact),
            OfficialUrl: NormalizeOptionalText(recall.URL));
    }

    private static string? JoinNames(IEnumerable<string?>? values)
    {
        if (values is null)
        {
            return null;
        }

        string[] items = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return items.Length == 0 ? null : string.Join(" ", items);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateQuery(ProductSafetyQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Manufacturer);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.ModelNumber);
    }

    private sealed class CpscRecallDto
    {
        [JsonPropertyName("RecallID")]
        public int RecallId { get; set; }

        public string? RecallNumber { get; set; }

        public DateTime? RecallDate { get; set; }

        public string? Description { get; set; }

        [JsonPropertyName("URL")]
        public string? URL { get; set; }

        public string? Title { get; set; }

        public string? ConsumerContact { get; set; }

        public List<CpscProductDto>? Products { get; set; }

        public List<CpscHazardDto>? Hazards { get; set; }

        public List<CpscRemedyDto>? Remedies { get; set; }
    }

    private sealed class CpscProductDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Model { get; set; }

        public string? Type { get; set; }
    }

    private sealed class CpscHazardDto
    {
        public string? Name { get; set; }
    }

    private sealed class CpscRemedyDto
    {
        public string? Name { get; set; }
    }
}