using ProductCare.Domain.Identity;

namespace ProductCare.Application.Access
{
    public sealed record AccessUser(
        UserIdentity Identity,
        string? Email,
        string? DisplayName);
}