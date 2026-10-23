using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.Products;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Products;
using ProductCare.Domain.Identity;
using ProductCare.Domain.Products;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        if (!TryParseConfidence(request.IdentificationConfidence, out ProductIdentificationConfidence? confidence))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid identification confidence.",
                Detail = "IdentificationConfidence must be Low, Medium, High, or null."
            });
        }

        NewProduct newProduct = new(
            request.Manufacturer,
            request.ModelNumber,
            request.ProductType,
            request.ProductName,
            request.SerialNumber,
            confidence);

        ProductRecord product = await _productService.CreateAsync(identity, newProduct, cancellationToken);

        ProductResponse response = ToResponse(product);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        IReadOnlyList<ProductRecord> products = await _productService.GetAllAsync(identity, cancellationToken);

        ProductResponse[] response = products.Select(ToResponse).ToArray();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productService.GetAsync(identity, id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(product));
    }

    private static ProductResponse ToResponse(ProductRecord product) => new(
        product.Id,
        product.Manufacturer,
        product.ModelNumber,
        product.ProductType,
        product.ProductName,
        product.SerialNumber,
        product.IdentificationConfidence,
        product.CreatedUtc);

    private static bool TryParseConfidence(string? value, out ProductIdentificationConfidence? confidence)
    {
        confidence = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out ProductIdentificationConfidence parsed) || !Enum.IsDefined(parsed))
        {
            return false;
        }

        confidence = parsed;

        return true;
    }
}