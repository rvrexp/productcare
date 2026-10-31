using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Accounts;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/account")]
[Authorize(Policy = ProductCarePolicies.User)]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountLifecycleService _accountLifecycleService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountLifecycleService accountLifecycleService, ILogger<AccountController> logger)
    {
        _accountLifecycleService = accountLifecycleService ?? throw new ArgumentNullException(nameof(accountLifecycleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        int deletedProducts = await _accountLifecycleService.DeleteAccountAsync(identity.TenantId, identity.UserId, cancellationToken);

        _logger.LogInformation("A user deleted their ProductCare account and {DeletedProductCount} owned products.", deletedProducts);

        return NoContent();
    }
}
