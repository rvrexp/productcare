
using Microsoft.AspNetCore.Authorization;

namespace ProductCare.Api.Authorization;

public sealed class NotDisabledRequirement : IAuthorizationRequirement
{
}