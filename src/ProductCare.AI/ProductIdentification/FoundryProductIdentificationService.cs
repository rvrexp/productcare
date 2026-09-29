using Azure.AI.Extensions.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using ProductCare.Application.ProductIdentification;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable OPENAI001

namespace ProductCare.AI.ProductIdentification;

public sealed class FoundryProductIdentificationService : IProductIdentificationService
{
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> SupportedMediaTypes = new(StringComparer.OrdinalIgnoreCase)
                                                                    {
                                                                        "image/jpeg",
                                                                        "image/png",
                                                                        "image/webp"
                                                                    };

    private static readonly BinaryData ProductIdentificationSchema = BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "manufacturer": {
                              "type": ["string", "null"]
                            },
                            "modelNumber": {
                              "type": ["string", "null"]
                            },
                            "productType": {
                              "type": ["string", "null"]
                            },
                            "productName": {
                              "type": ["string", "null"]
                            },
                            "confidence": {
                              "type": "string",
                              "enum": ["Low", "Medium", "High"]
                            },
                            "observedText": {
                              "type": "array",
                              "items": {
                                "type": "string"
                              }
                            }
                          },
                          "required": [
                            "manufacturer",
                            "modelNumber",
                            "productType",
                            "productName",
                            "confidence",
                            "observedText"
                          ],
                          "additionalProperties": false
                        }
                        """);

    private const string ProductIdentificationInstructions =
        """
        You identify consumer products from product photographs
        and product-label photographs.

        Determine:
        - manufacturer
        - model number
        - product type
        - product name
        - confidence
        - text visibly observed in the image

        Do not invent a manufacturer, model number, or product name.

        If a value cannot be reliably determined from the image,
        return null for that value.

        ObservedText must contain only text actually visible
        in the supplied image.

        Do not add inferred text to ObservedText.

        Confidence must be Low, Medium, or High.
        """;

    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private readonly ProjectResponsesClient _responsesClient;
    private readonly ILogger<FoundryProductIdentificationService> _logger;

    public FoundryProductIdentificationService(ProjectResponsesClient responsesClient, ILogger<FoundryProductIdentificationService> logger)
    {
        _responsesClient = responsesClient ?? throw new ArgumentNullException(nameof(responsesClient));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductIdentificationResult> IdentifyAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        ValidateImage(image);

        CreateResponseOptions options = CreateResponseOptions(image.Content, image.MediaType);

        long startTimestamp = Stopwatch.GetTimestamp();

        ResponseResult response =  await _responsesClient.CreateResponseAsync(options, cancellationToken);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        string json = response.GetOutputText();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Foundry returned an empty response.");
        }

        ProductIdentificationResult result;

        try
        {
            result = JsonSerializer.Deserialize<ProductIdentificationResult>(json, SerializerOptions) ?? throw new InvalidOperationException(
                    "Foundry returned an invalid product identification result.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Foundry returned a product identification result " +
                "that could not be deserialized.",
                exception);
        }

        _logger.LogInformation("Product identification completed in {ElapsedMilliseconds} ms " +
            "with {Confidence} confidence.", elapsed.TotalMilliseconds, result.Confidence);

        return result;
    }

    private static CreateResponseOptions CreateResponseOptions(byte[] imageContent, string mediaType) =>
        new()
        {
            Instructions = ProductIdentificationInstructions,

            InputItems =
            {
                ResponseItem.CreateUserMessageItem(
                [
                    ResponseContentPart.CreateInputTextPart(
                        "Identify the product shown in this image."),

                    ResponseContentPart.CreateInputImagePart(
                        new Uri( CreateImageDataUrl(imageContent, mediaType),UriKind.Absolute),
                        ResponseImageDetailLevel.High)
                ])
            },

            TextOptions = new ResponseTextOptions
            {
                TextFormat =
                    ResponseTextFormat.CreateJsonSchemaFormat(
                        jsonSchemaFormatName:
                            "product_identification",
                        jsonSchema:
                            ProductIdentificationSchema,
                        jsonSchemaIsStrict:
                            true)
            }
        };

    private static string CreateImageDataUrl(byte[] imageContent, string mediaType) => 
                                        $"data:{mediaType};base64," + Convert.ToBase64String(imageContent);

    private static void ValidateImage(ProductImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        ArgumentNullException.ThrowIfNull(image.Content);

        if (image.Content.Length == 0)
        {
            throw new ArgumentException("Image content cannot be empty.", nameof(image));
        }

        if (image.Content.Length > MaxImageSizeBytes)
        {
            throw new ArgumentException("Image content cannot exceed 5 MB.", nameof(image));
        }

        if (string.IsNullOrWhiteSpace(image.MediaType))
        {
            throw new ArgumentException("Image media type is required.", nameof(image));
        }

        if (!SupportedMediaTypes.Contains(image.MediaType))
        {
            throw new ArgumentException("Only JPEG, PNG, and WebP images are supported.", nameof(image));
        }
    }
}

#pragma warning restore OPENAI001