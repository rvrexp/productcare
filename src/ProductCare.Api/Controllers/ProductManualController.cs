using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.ProductManuals;
using ProductCare.Application.Products;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/manual")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class ProductManualController : ControllerBase
{
    private const long MaxManualFileSize = 15 * 1024 * 1024;
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    private readonly IProductService _productService;
    private readonly IProductManualIndexService _manualIndexService;

    public ProductManualController(IProductService productService, IProductManualIndexService manualIndexService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _manualIndexService = manualIndexService ?? throw new ArgumentNullException(nameof(manualIndexService));
    }

    [HttpGet]
    public async Task<ActionResult<ProductManualStatusResponse>> GetStatus(Guid productId, CancellationToken cancellationToken)
    {
        ProductRecord? product = await GetOwnedProductAsync(productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        ProductManualStatus status = await _manualIndexService.GetStatusAsync(product.Id, cancellationToken);

        return Ok(ToResponse(status));
    }

    [HttpPost]
    [RequestSizeLimit(MaxManualFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxManualFileSize)]
    public async Task<ActionResult<ProductManualStatusResponse>> Upload(Guid productId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        ProductRecord? product = await GetOwnedProductAsync(productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(product.Manufacturer) || string.IsNullOrWhiteSpace(product.ModelNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product information is incomplete.",
                Detail = "A confirmed manufacturer and model number are required before uploading a product manual.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "A PDF manual is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (file.Length > MaxManualFileSize)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The manual is too large.",
                Detail = "Product manuals cannot exceed 15 MB.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Only PDF manuals are supported.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using MemoryStream content = new((int)file.Length);
        await file.CopyToAsync(content, cancellationToken);

        if (!HasPdfSignature(content))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The uploaded file is not a valid PDF.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        content.Position = 0;

        ProductManualStatus status = await _manualIndexService.UploadAsync(product.Id, product.Manufacturer, product.ModelNumber, content, cancellationToken);

        return Accepted(ToResponse(status));
    }

    [HttpPost("retry")]
    public async Task<ActionResult<ProductManualStatusResponse>> Retry(Guid productId, CancellationToken cancellationToken)
    {
        ProductRecord? product = await GetOwnedProductAsync(productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        ProductManualStatus status = await _manualIndexService.RetryAsync(product.Id, cancellationToken);

        return Accepted(ToResponse(status));
    }

    private async Task<ProductRecord?> GetOwnedProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return null;
        }

        return await _productService.GetAsync(identity, productId, cancellationToken);
    }

    private static bool HasPdfSignature(MemoryStream content)
    {
        if (content.Length < PdfSignature.Length)
        {
            return false;
        }

        ReadOnlySpan<byte> bytes = content.GetBuffer().AsSpan(0, PdfSignature.Length);

        return bytes.SequenceEqual(PdfSignature);
    }

    private static ProductManualStatusResponse ToResponse(ProductManualStatus status) =>
        new(status.State.ToString(), status.State != ProductManualState.NotUploaded, status.State == ProductManualState.Ready, status.CanRetry, status.RetryAfterUtc);
}

public sealed record ProductManualStatusResponse(string State, bool Uploaded, bool Ready, bool CanRetry, DateTime? RetryAfterUtc);