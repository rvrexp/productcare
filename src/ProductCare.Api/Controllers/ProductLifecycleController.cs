using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Accounts;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class ProductLifecycleController : ControllerBase
{
    private readonly IAccountLifecycleService _accountLifecycleService;
    private readonly ILogger<ProductLifecycleController> _logger;

    public ProductLifecycleController(IAccountLifecycleService accountLifecycleService, ILogger<ProductLifecycleController> logger)
    {
        _accountLifecycleService = accountLifecycleService ?? throw new ArgumentNullException(nameof(accountLifecycleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        bool deleted = await _accountLifecycleService.DeleteProductAsync(identity.TenantId, identity.UserId, productId, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        _logger.LogInformation("Product {ProductId} and its dependent ProductCare resources were deleted by its owner.", productId);

        return NoContent();
    }
}
