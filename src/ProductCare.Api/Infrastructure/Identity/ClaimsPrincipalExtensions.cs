using ProductCare.Application.Access;
using ProductCare.Domain.Identity;
using System.Security.Claims;

namespace ProductCare.Api.Infrastructure.Identity
{
    public static class ClaimsPrincipalExtensions
    {
        private const string TenantIdClaim = "tid";
        private const string MappedTenantIdClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string ObjectIdClaim = "oid";
        private const string MappedObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string EmailClaim = "email";
        private const string EmailsClaim = "emails";
        private const string NameClaim = "name";

        public static bool TryGetUserIdentity(this ClaimsPrincipal principal, out UserIdentity? identity)
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? tenantId = principal.FindFirst(TenantIdClaim)?.Value ?? principal.FindFirst(MappedTenantIdClaim)?.Value;
            string? userId = principal.FindFirst(ObjectIdClaim)?.Value ?? principal.FindFirst(MappedObjectIdClaim)?.Value;

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId))
            {
                identity = null;
                return false;
            }

            identity = new UserIdentity(tenantId, userId);

            return true;
        }
        public static bool TryGetAccessUser(this ClaimsPrincipal principal, out AccessUser? user)
        {
            ArgumentNullException.ThrowIfNull(principal);

            if (!principal.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
            {
                user = null;
                return false;
            }

            string? email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst(EmailClaim)?.Value
                ?? principal.FindFirst(EmailsClaim)?.Value;

            string? displayName = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst(NameClaim)?.Value;

            user = new AccessUser(identity, email, displayName);

            return true;
        }
    }
}