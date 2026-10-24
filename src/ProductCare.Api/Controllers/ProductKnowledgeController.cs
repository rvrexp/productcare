using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.ProductKnowledge;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.ProductKnowledge;
using ProductCare.Application.Products;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;
using System.Security.Principal;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/questions")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class ProductKnowledgeController : ControllerBase
{
    private readonly IUsageQuotaService _usageQuotaService;
    private readonly IProductService _productService;
    private readonly IProductKnowledgeService _productKnowledgeService;

    public ProductKnowledgeController(IProductService productService, IProductKnowledgeService productKnowledgeService, IUsageQuotaService usageQuotaService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _productKnowledgeService = productKnowledgeService ?? throw new ArgumentNullException(nameof(productKnowledgeService));
        _usageQuotaService = usageQuotaService ?? throw new ArgumentNullException(nameof(usageQuotaService));   
    }

    [HttpPost]
    public async Task<ActionResult<ProductKnowledgeResponse>> Ask(Guid productId, AskProductQuestionRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        ProductRecord? product = await _productService.GetAsync(identity, productId, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(product.Manufacturer) || string.IsNullOrWhiteSpace(product.ModelNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product model information is required.",
                Detail = "A manufacturer and model number are required before ProductCare can search product documentation."
            });
        }

        ProductKnowledgeQuery query = new(product.Manufacturer, product.ModelNumber, request.Question);

        await _usageQuotaService.ConsumeAsync(identity, UsageOperation.ProductKnowledge, cancellationToken);

        ProductKnowledgeAnswer answer = await _productKnowledgeService.AskAsync(query, cancellationToken);

        ProductKnowledgeResponse response = new(
            answer.Answer,
            answer.Citations.Select(citation => new ProductKnowledgeCitationResponse(citation.SourceId, citation.Title)).ToArray());

        return Ok(response);
    }
}