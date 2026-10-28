using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProductCare.Web.Api;

public sealed class ProductCareApiClient
{
    private const long MaxImageFileSize = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public ProductCareApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<UserAccessResponse?> GetUserAccessAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/access/me", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserAccessResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductAssistantResponse> StreamProductAssistantAsync(Guid productId, string message, Func<string, Task> onDelta, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(onDelta);

        using HttpRequestMessage request = new(HttpMethod.Post, $"api/v1/products/{productId}/assistant/stream")
        {
            Content = JsonContent.Create(new AskProductAssistantRequest(message))
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        await ThrowIfQuotaExceededAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(responseStream);

        ProductAssistantResponse? completedResult = null;

        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ProductAssistantStreamEnvelope? streamEvent = JsonSerializer.Deserialize<ProductAssistantStreamEnvelope>(line, StreamJsonOptions);

            if (streamEvent is null)
            {
                continue;
            }

            if (string.Equals(streamEvent.Type, "delta", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(streamEvent.Text))
            {
                await onDelta(streamEvent.Text);
            }
            else if (string.Equals(streamEvent.Type, "completed", StringComparison.OrdinalIgnoreCase) && streamEvent.Result is not null)
            {
                completedResult = streamEvent.Result;
            }
        }

        return completedResult ?? throw new InvalidOperationException("ProductCare Assistant streaming response ended before the final result was received.");
    }

    public async Task StartNewProductAssistantConversationAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/products/{productId}/assistant/new-conversation", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserAccessResponse?> SubmitAccessRequestAsync(SubmitAccessRequestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/v1/access/requests", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserAccessResponse>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAccessRequestResponse>> GetPendingAccessRequestsAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/admin/access-requests", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<AdminAccessRequestResponse>? requests = await response.Content.ReadFromJsonAsync<List<AdminAccessRequestResponse>>(cancellationToken: cancellationToken);

        return requests ?? [];
    }

    public async Task<AdminAccessRequestResponse?> ApproveAccessRequestAsync(Guid requestId, DateTime accessExpiresUtc, CancellationToken cancellationToken = default)
    {
        ApproveAdminAccessRequestRequest request = new(accessExpiresUtc);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/v1/admin/access-requests/{requestId}/approve", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdminAccessRequestResponse>(cancellationToken: cancellationToken);
    }

    public async Task<AdminAccessRequestResponse?> RejectAccessRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.PostAsync($"api/v1/admin/access-requests/{requestId}/reject", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdminAccessRequestResponse>(cancellationToken: cancellationToken);
    }

    public async Task<SystemStatusResponse?> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/system/status", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SystemStatusResponse>(cancellationToken: cancellationToken);
    }

    public async Task<UsageQuotaSummaryResponse?> GetUsageQuotaSummaryAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/usage/me", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UsageQuotaSummaryResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductIdentificationResponse?> IdentifyProductAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using Stream fileStream = file.OpenReadStream(MaxImageFileSize, cancellationToken);
        using StreamContent fileContent = new(fileStream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        using MultipartFormDataContent form = new();
        form.Add(fileContent, "image", file.Name);

        using HttpResponseMessage response = await _httpClient.PostAsync("api/v1/products/identify", form, cancellationToken);

        await ThrowIfQuotaExceededAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductIdentificationResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/v1/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/v1/products", cancellationToken);
        response.EnsureSuccessStatusCode();

        List<ProductResponse>? products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(cancellationToken: cancellationToken);

        return products ?? [];
    }

    public async Task<ProductResponse?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductKnowledgeResponse?> AskProductQuestionAsync(Guid productId, string question, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        AskProductQuestionRequest request = new(question);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/v1/products/{productId}/questions", request, cancellationToken);

        await ThrowIfQuotaExceededAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductKnowledgeResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductSafetyResponse?> GetProductSafetyAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync($"api/v1/products/{productId}/safety", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductSafetyResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ProductAssistantResponse?> AskProductAssistantAsync(Guid productId, string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        AskProductAssistantRequest request = new(message);

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/v1/products/{productId}/assistant", request, cancellationToken);

        await ThrowIfQuotaExceededAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductAssistantResponse>(cancellationToken: cancellationToken);
    }

    private static async Task ThrowIfQuotaExceededAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return;
        }

        string? operation = null;
        int? dailyLimit = null;
        DateTime? retryAfterUtc = null;

        try
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(json))
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("operation", out JsonElement operationElement) && operationElement.ValueKind == JsonValueKind.String)
                {
                    operation = operationElement.GetString();
                }

                if (root.TryGetProperty("dailyLimit", out JsonElement dailyLimitElement) && dailyLimitElement.TryGetInt32(out int parsedDailyLimit))
                {
                    dailyLimit = parsedDailyLimit;
                }

                if (root.TryGetProperty("retryAfterUtc", out JsonElement retryAfterElement) && retryAfterElement.ValueKind == JsonValueKind.String && DateTime.TryParse(retryAfterElement.GetString(), out DateTime parsedRetryAfterUtc))
                {
                    retryAfterUtc = parsedRetryAfterUtc.Kind == DateTimeKind.Utc ? parsedRetryAfterUtc : parsedRetryAfterUtc.ToUniversalTime();
                }
            }
        }
        catch (JsonException)
        {
        }

        throw new ProductCareQuotaExceededException(operation, dailyLimit, retryAfterUtc);
    }

    private sealed record AskProductQuestionRequest(string Question);

    private sealed record AskProductAssistantRequest(string Message);

    private sealed record ProductAssistantStreamEnvelope(string Type, string? Text, ProductAssistantResponse? Result);
}

public sealed record SystemStatusResponse(string Status);