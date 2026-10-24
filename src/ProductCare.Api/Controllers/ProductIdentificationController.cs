using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.ProductIdentification;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.ProductIdentification;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    [Authorize(Policy = ProductCarePolicies.ActiveUser)]
    public sealed class ProductIdentificationController : ControllerBase
    {
        private const long MaxImageSize = 5 * 1024 * 1024;
        private readonly IUsageQuotaService _usageQuotaService;
        private static readonly HashSet<string> AllowedContentTypes =new(StringComparer.OrdinalIgnoreCase)
                                                                            {
                                                                            "image/jpeg",
                                                                            "image/png",
                                                                            "image/webp"
                                                                            };

        private readonly IProductIdentificationService _productIdentificationService;

        public ProductIdentificationController(IProductIdentificationService productIdentificationService, IUsageQuotaService usageQuotaService)
        {
            _productIdentificationService = productIdentificationService;
            _usageQuotaService = usageQuotaService ?? throw new ArgumentNullException(nameof(usageQuotaService));
        }

        [HttpPost("identify")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductIdentificationResponse>> Identify(IFormFile image, CancellationToken cancellationToken)
        {
            if (image is null || image.Length == 0)
            {
                return BadRequest("An image is required.");
            }

            if (image.Length > MaxImageSize)
            {
                return BadRequest("The image must be 5 MB or smaller.");
            }

            if (!AllowedContentTypes.Contains(image.ContentType))
            {
                return BadRequest("Only JPEG, PNG, and WebP images are supported.");
            }

            await using Stream stream = image.OpenReadStream();
            using MemoryStream memoryStream = new();

            await stream.CopyToAsync(memoryStream, cancellationToken);

            byte[] imageBytes = memoryStream.ToArray();

            if (!HasValidImageSignature(imageBytes, image.ContentType))
            {
                return BadRequest("The uploaded file is not a valid image.");
            }

            ProductImage productImage = new(imageBytes, image.ContentType);

            if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
            {
                return Forbid();
            }

            await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);


            ProductIdentificationResult result = await _productIdentificationService.IdentifyAsync(productImage, cancellationToken);
             

            ProductIdentificationResponse response = new(
                result.Manufacturer,
                result.ModelNumber,
                result.ProductType,
                result.ProductName,
                result.Confidence,
                result.ObservedText);

            return Ok(response);
        }

        private static bool HasValidImageSignature(byte[] data, string contentType)
        {
            if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return data.Length >= 3
                    && data[0] == 0xFF
                    && data[1] == 0xD8
                    && data[2] == 0xFF;
            }

            if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            {
                byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

                return data.Length >= signature.Length && data.Take(signature.Length).SequenceEqual(signature);
            }

            if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            {
                return data.Length >= 12
                    && data[0] == (byte)'R'
                    && data[1] == (byte)'I'
                    && data[2] == (byte)'F'
                    && data[3] == (byte)'F'
                    && data[8] == (byte)'W'
                    && data[9] == (byte)'E'
                    && data[10] == (byte)'B'
                    && data[11] == (byte)'P';
            }

            return false;
        }
    }
}
