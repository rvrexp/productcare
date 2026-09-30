using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Contracts.Products;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.ProductIdentification;
using ProductCare.Application.Products;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductCatalog _productCatalog;

    public ProductsController(IProductCatalog productCatalog)
    {
        _productCatalog = productCatalog ?? throw new ArgumentNullException(nameof(productCatalog));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetProductOwner(out ProductOwner? owner) || owner is null)
        {
            return Forbid();
        }

        if (!TryParseConfidence(request.IdentificationConfidence, out ProductIdentificationConfidence? confidence))
        {
            return BadRequest(
                new ProblemDetails
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

        ProductRecord product = await _productCatalog.AddAsync( owner, newProduct, cancellationToken);

        ProductResponse response = ToResponse(product);

        return CreatedAtAction( nameof(GetById), new
                                                    {
                                                        id = product.Id
                                                    },
                                                    response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!User.TryGetProductOwner(out ProductOwner? owner) || owner is null)
        {
            return Forbid();
        }

        IReadOnlyList<ProductRecord> products = await _productCatalog.GetAllAsync( owner, cancellationToken);

        ProductResponse[] response = products.Select(ToResponse).ToArray();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetProductOwner( out ProductOwner? owner) || owner is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productCatalog.GetAsync( owner, id, cancellationToken);


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

        if (!Enum.TryParse( value, ignoreCase: true, out ProductIdentificationConfidence parsed) 
                || !Enum.IsDefined( typeof( ProductIdentificationConfidence), parsed))
        {
            return false;
        }

        confidence = parsed;

        return true;
    }
}