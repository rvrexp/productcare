using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.Admin;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/admin/access-requests")]
[Authorize(Policy = ProductCarePolicies.Admin)]
public sealed class AdminAccessRequestsController : ControllerBase
{
    private readonly IUserAccessService _userAccessService;

    public AdminAccessRequestsController(IUserAccessService userAccessService)
    {
        _userAccessService = userAccessService ?? throw new ArgumentNullException(nameof(userAccessService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccessRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccessRequestResponse>>> GetPendingRequests(CancellationToken cancellationToken)
    {
        IReadOnlyList<AccessRequestRecord> requests = await _userAccessService.GetPendingRequestsAsync(cancellationToken);

        return Ok(requests.Select(ToResponse).ToList());
    }

    [HttpPost("{requestId:guid}/approve")]
    [ProducesResponseType(typeof(AccessRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccessRequestResponse>> Approve(Guid requestId, ApproveAccessRequestRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? reviewer) || reviewer is null)
        {
            return Forbid();
        }

        if (request.AccessExpiresUtc.Kind != DateTimeKind.Utc)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The expiration is invalid.",
                Detail = "AccessExpiresUtc must be supplied as a UTC date and time."
            });
        }

        AccessRequestRecord? accessRequest = await _userAccessService.ApproveAsync(requestId, reviewer, request.AccessExpiresUtc, cancellationToken);

        if (accessRequest is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(accessRequest));
    }

    [HttpPost("{requestId:guid}/reject")]
    [ProducesResponseType(typeof(AccessRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccessRequestResponse>> Reject(Guid requestId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? reviewer) || reviewer is null)
        {
            return Forbid();
        }

        AccessRequestRecord? accessRequest = await _userAccessService.RejectAsync(requestId, reviewer, cancellationToken);

        if (accessRequest is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(accessRequest));
    }

    private static AccessRequestResponse ToResponse(AccessRequestRecord request) => new(
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