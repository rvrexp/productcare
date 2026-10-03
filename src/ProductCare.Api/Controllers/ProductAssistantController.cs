using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Contracts.ProductAssistant;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.ProductAssistant;
using ProductCare.Application.Products;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/assistant")]
[Authorize]
public sealed class ProductAssistantController : ControllerBase
{
    private readonly IProductCatalog _productCatalog;
    private readonly IProductAssistantService _productAssistantService;

    public ProductAssistantController(IProductCatalog productCatalog, IProductAssistantService productAssistantService)
    {
        _productCatalog = productCatalog ?? throw new ArgumentNullException(nameof(productCatalog));
        _productAssistantService = productAssistantService ?? throw new ArgumentNullException(nameof(productAssistantService));
    }

    [HttpPost]
    public async Task<ActionResult<ProductAssistantResponse>> Ask(Guid productId, AskProductAssistantRequest request, CancellationToken cancellationToken)
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
                Detail = "A confirmed manufacturer and model number are required before ProductCare Assistant can answer product-specific questions."
            });
        }

        ProductAssistantQuery query = new(product.Manufacturer, product.ModelNumber, product.ProductName, request.Message);
        ProductAssistantResult result = await _productAssistantService.AskAsync(query, cancellationToken);

        ProductAssistantSourceResponse[] sources = result.Sources.Select(source => new ProductAssistantSourceResponse(source.Type.ToString(), source.Title, source.Url)).ToArray();

        return Ok(new ProductAssistantResponse(result.Answer, sources, result.ToolsUsed));
    }
}