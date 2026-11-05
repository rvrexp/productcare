using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.Admin;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Access;
using ProductCare.Application.Accounts;
using ProductCare.Application.Administration;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = ProductCarePolicies.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserAdministrationService _userAdministrationService;
    private readonly IAccountLifecycleService _accountLifecycleService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IUserAdministrationService userAdministrationService,
        IAccountLifecycleService accountLifecycleService,
        ILogger<AdminUsersController> logger)
    {
        _userAdministrationService = userAdministrationService ?? throw new ArgumentNullException(nameof(userAdministrationService));
        _accountLifecycleService = accountLifecycleService ?? throw new ArgumentNullException(nameof(accountLifecycleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> List(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        IReadOnlyList<AdminUserRecord> users = await _userAdministrationService.ListAsync(cancellationToken);

        return Ok(users.Select(user => ToResponse(user, actor)).ToArray());
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<AdminUserDetailsResponse>> Get(Guid accountId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        AdminUserDetailsRecord? user = await _userAdministrationService.GetDetailsAsync(accountId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(ToDetailsResponse(user, actor));
    }

    [HttpPost("{accountId:guid}/suspend")]
    public async Task<ActionResult<AdminUserResponse>> Suspend(Guid accountId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        AdminUserRecord? target = await _userAdministrationService.GetAsync(accountId, cancellationToken);

        if (target is null)
        {
            return NotFound();
        }

        if (IsSameUser(target.TenantId, target.UserId, actor))
        {
            return Conflict(new ProblemDetails
            {
                Title = "You cannot suspend your own administrator account.",
                Detail = "Use another administrator account for this operation."
            });
        }

        AdminUserRecord updated = (await _userAdministrationService.SuspendAsync(accountId, cancellationToken))!;
        _logger.LogWarning("Administrator suspended ProductCare account {AccountId}.", accountId);

        return Ok(ToResponse(updated, actor));
    }

    [HttpPost("{accountId:guid}/restore")]
    public async Task<ActionResult<AdminUserResponse>> Restore(Guid accountId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        AdminUserRecord? target = await _userAdministrationService.GetAsync(accountId, cancellationToken);

        if (target is null)
        {
            return NotFound();
        }

        if (IsSameUser(target.TenantId, target.UserId, actor))
        {
            return Conflict(new ProblemDetails
            {
                Title = "You cannot change your own administrator access from this screen.",
                Detail = "Use another administrator account for this operation."
            });
        }

        AdminUserRecord updated = (await _userAdministrationService.RestoreAsync(accountId, cancellationToken))!;
        _logger.LogWarning("Administrator restored ProductCare account {AccountId} to status {Status}.", accountId, updated.Status);

        return Ok(ToResponse(updated, actor));
    }

    [HttpPost("{accountId:guid}/purge")]
    public async Task<IActionResult> Purge(Guid accountId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        AdminUserRecord? target = await _userAdministrationService.GetAsync(accountId, cancellationToken);

        if (target is null)
        {
            return NotFound();
        }

        if (IsSameUser(target.TenantId, target.UserId, actor))
        {
            return Conflict(new ProblemDetails
            {
                Title = "You cannot purge your own administrator account from the admin user screen.",
                Detail = "Use the Account page for self-service ProductCare account deletion."
            });
        }

        await _userAdministrationService.SuspendAsync(accountId, cancellationToken);
        int deletedProducts = await _accountLifecycleService.PurgeAccountAsync(target.TenantId, target.UserId, cancellationToken);

        _logger.LogWarning("Administrator purged ProductCare data for account {AccountId}, deleted {DeletedProductCount} owned products, and kept the account disabled.", accountId, deletedProducts);

        return NoContent();
    }

    [HttpDelete("{accountId:guid}")]
    public async Task<IActionResult> Delete(Guid accountId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? actor) || actor is null)
        {
            return Forbid();
        }

        AdminUserRecord? target = await _userAdministrationService.GetAsync(accountId, cancellationToken);

        if (target is null)
        {
            return NotFound();
        }

        if (IsSameUser(target.TenantId, target.UserId, actor))
        {
            return Conflict(new ProblemDetails
            {
                Title = "You cannot delete your own administrator account from the admin user screen.",
                Detail = "Use the Account page for self-service ProductCare account deletion."
            });
        }

        int deletedProducts = await _accountLifecycleService.DeleteAccountAsync(target.TenantId, target.UserId, cancellationToken);

        _logger.LogWarning("Administrator deleted ProductCare account {AccountId} and {DeletedProductCount} owned products. The external Entra identity was not deleted.", accountId, deletedProducts);

        return NoContent();
    }

    private static bool IsSameUser(string tenantId, string userId, UserIdentity actor) =>
        tenantId.Equals(actor.TenantId, StringComparison.OrdinalIgnoreCase) &&
        userId.Equals(actor.UserId, StringComparison.Ordinal);

    private static AdminUserResponse ToResponse(AdminUserRecord user, UserIdentity actor) =>
        new(
            user.Id,
            user.DisplayName,
            user.Email,
            user.Status.ToString(),
            user.Plan?.ToString(),
            user.AccessExpiresUtc,
            user.CreatedUtc,
            user.UpdatedUtc,
            IsSameUser(user.TenantId, user.UserId, actor));

    private static AdminUserDetailsResponse ToDetailsResponse(AdminUserDetailsRecord user, UserIdentity actor) =>
        new(
            user.Id,
            user.DisplayName,
            user.Email,
            user.Status.ToString(),
            user.Plan?.ToString(),
            user.AccessExpiresUtc,
            user.CreatedUtc,
            user.UpdatedUtc,
            IsSameUser(user.TenantId, user.UserId, actor),
            user.AccessRequests.Select(ToAccessRequestResponse).ToArray());

    private static AccessRequestResponse ToAccessRequestResponse(AccessRequestRecord request) =>
        new(
            request.Id,
            request.Email,
            request.DisplayName,
            request.Organization,
            request.Purpose,
            request.Message,
            request.Status.ToString(),
            request.RequestedUtc,
            request.ReviewedUtc);
}
