
using Microsoft.AspNetCore.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Access;
using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Authorization;

public sealed class NotDisabledAuthorizationHandler(
    IUserAccessService userAccessService) : AuthorizationHandler<NotDisabledRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotDisabledRequirement requirement)
    {
        if (!context.User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return;
        }

        CancellationToken cancellationToken = context.Resource is HttpContext httpContext ? httpContext.RequestAborted : CancellationToken.None;
        UserAccessRecord access = await userAccessService.GetStatusAsync(identity, cancellationToken);

        if (access.Status != UserAccountStatus.Disabled)
        {
            context.Succeed(requirement);
        }
    }
}
