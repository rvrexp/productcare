using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Contracts.ProductSafety;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Products;
using ProductCare.Application.ProductSafety;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/safety")]
[Authorize]
public sealed class ProductSafetyController : ControllerBase
{
    private readonly IProductCatalog _productCatalog;
    private readonly IProductSafetyService _productSafetyService;

    public ProductSafetyController(IProductCatalog productCatalog, IProductSafetyService productSafetyService)
    {
        _productCatalog = productCatalog ?? throw new ArgumentNullException(nameof(productCatalog));
        _productSafetyService = productSafetyService ?? throw new ArgumentNullException(nameof(productSafetyService));
    }

    [HttpGet]
    public async Task<ActionResult<ProductSafetyResponse>> Get(Guid productId, CancellationToken cancellationToken)
    {
        if (!User.TryGetProductOwner(out ProductOwner? owner) || owner is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productCatalog.GetAsync(owner, productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(product.Manufacturer) || string.IsNullOrWhiteSpace(product.ModelNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product model information is required.",
                Detail = "A manufacturer and model number are required before ProductCare can perform a reliable recall check."
            });
        }

        ProductSafetyQuery query = new(product.Manufacturer, product.ModelNumber, product.ProductName);
        ProductSafetyCheckResult result = await _productSafetyService.CheckAsync(query, cancellationToken);

        if (!result.SourceAvailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Recall information is temporarily unavailable.",
                Detail = "ProductCare could not reach the CPSC recall service. Try again later."
            });
        }

        ProductRecallResponse[] recalls = result.Recalls.Select(recall => new ProductRecallResponse(
            recall.RecallNumber,
            recall.Title,
            recall.RecallDate,
            recall.Description,
            recall.Hazard,
            recall.Remedy,
            recall.ConsumerContact,
            recall.OfficialUrl)).ToArray();

        return Ok(new ProductSafetyResponse(result.CheckedUtc, recalls));
    }
}