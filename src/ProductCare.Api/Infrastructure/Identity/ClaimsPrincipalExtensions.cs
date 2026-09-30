using ProductCare.Application.Products;
using System.Security.Claims;

namespace ProductCare.Api.Infrastructure.Identity
{
    public static class ClaimsPrincipalExtensions
    {
        private const string TenantIdClaim = "tid";

        private const string MappedTenantIdClaim = "http://schemas.microsoft.com/identity/claims/tenantid";

        public static bool TryGetProductOwner(this ClaimsPrincipal principal, out ProductOwner? owner)
        {
            ArgumentNullException.ThrowIfNull(principal);

            string? tenantId = principal.FindFirst(TenantIdClaim)?.Value ?? principal.FindFirst(MappedTenantIdClaim)?.Value;

            string? subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(subject))
            {
                owner = null;
                return false;
            }

            owner = new ProductOwner(tenantId, subject);

            return true;
        }
    }
}
