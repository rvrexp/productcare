using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductCare.Api.Authorization;
using ProductCare.Api.Contracts.Access;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Api.RateLimiting;
using ProductCare.Application.Access;
using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/access")]
[Authorize(Policy = ProductCarePolicies.User)]
public sealed class AccessController : ControllerBase
{
    private readonly IUserAccessService _userAccessService;

    public AccessController(IUserAccessService userAccessService)
    {
        _userAccessService = userAccessService ?? throw new ArgumentNullException(nameof(userAccessService));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserAccessResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserAccessResponse>> GetMyAccess(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        UserAccessRecord access = await _userAccessService.GetStatusAsync(identity, cancellationToken);

        return Ok(ToResponse(access, User.IsInRole(ProductCarePolicies.AdministratorRole)));
    }

    [HttpPost("requests")]
    [EnableRateLimiting(ProductCareRateLimitPolicies.AccessRequestSubmission)]
    [ProducesResponseType(typeof(UserAccessResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAccessResponse>> SubmitRequest(SubmitAccessRequestRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetAccessUser(out AccessUser? user) || user is null)
        {
            return Forbid();
        }

        string purpose = request.Purpose.Trim();

        if (!AccessRequestPurposes.IsSupported(purpose))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid access request purpose.",
                Detail = "Purpose must be Portfolio / Interview Review, Technical Evaluation, or Other."
            });
        }

        NewAccessRequest newRequest = new(
            request.Organization,
            purpose,
            request.Message);

        UserAccessRecord access = await _userAccessService.SubmitRequestAsync(user, newRequest, cancellationToken);

        return Accepted(ToResponse(access, User.IsInRole(ProductCarePolicies.AdministratorRole)));
    }

    private static UserAccessResponse ToResponse(UserAccessRecord access, bool isAdmin) => new(
        access.Status.ToString(),
        access.Plan?.ToString(),
        access.AccessExpiresUtc,
        access.RequestedUtc,
        isAdmin);
}