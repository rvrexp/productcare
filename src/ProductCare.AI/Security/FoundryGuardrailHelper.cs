using System.ClientModel;
using System.Text.Json;

namespace ProductCare.AI.Security;

internal static class FoundryGuardrailHelper
{
    private const string ContentFilterCode = "content_filter";

    public static bool IsBlockedByContentFilter(ClientResultException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.Status != 400)
        {
            return false;
        }

        try
        {
            var response = exception.GetRawResponse();

            if (response is null)
            {
                return exception.Message.Contains(ContentFilterCode, StringComparison.OrdinalIgnoreCase);
            }

            var content = response.Content;

            if (content.ToMemory().IsEmpty)
            {
                return exception.Message.Contains(ContentFilterCode, StringComparison.OrdinalIgnoreCase);
            }

            using JsonDocument document = JsonDocument.Parse(content.ToMemory());

            if (!document.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                return false;
            }

            if (!errorElement.TryGetProperty("code", out JsonElement codeElement) || codeElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return string.Equals(codeElement.GetString(), ContentFilterCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return exception.Message.Contains(ContentFilterCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidOperationException)
        {
            return exception.Message.Contains(ContentFilterCode, StringComparison.OrdinalIgnoreCase);
        }
    }
}
