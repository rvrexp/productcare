using Microsoft.AspNetCore.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Authorization
{
    public sealed class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
    {
        private readonly IUserAccessService _userAccessService;

        public ActiveUserAuthorizationHandler(IUserAccessService userAccessService)
        {
            _userAccessService = userAccessService ?? throw new ArgumentNullException(nameof(userAccessService));
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveUserRequirement requirement)
        {
            if (!context.User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
            {
                return;
            }

            CancellationToken cancellationToken = context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None;

            if (await _userAccessService.HasActiveAccessAsync(identity, cancellationToken))
            {
                context.Succeed(requirement);
            }
        }
    }
}