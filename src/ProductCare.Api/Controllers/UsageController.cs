using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCare.Api.Authorization;
using ProductCare.Api.Infrastructure.Identity;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;

namespace ProductCare.Api.Controllers;

[ApiController]
[Route("api/v1/usage")]
[Authorize(Policy = ProductCarePolicies.ActiveUser)]
public sealed class UsageController : ControllerBase
{
    private readonly IUsageQuotaService _usageQuotaService;

    public UsageController(IUsageQuotaService usageQuotaService)
    {
        _usageQuotaService = usageQuotaService ?? throw new ArgumentNullException(nameof(usageQuotaService));
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UsageQuotaSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageQuotaSummaryResponse>> GetMyUsage(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserIdentity(out UserIdentity? identity) || identity is null)
        {
            return Forbid();
        }

        UsageQuotaSummary summary = await _usageQuotaService.GetSummaryAsync(identity, cancellationToken);

        UsageQuotaOperationResponse[] operations = summary.Operations.Where(operation => operation.Operation != UsageOperation.ProductKnowledge)
            .Select(operation => new UsageQuotaOperationResponse(
                operation.Operation.ToString(),
                GetDisplayName(operation.Operation),
                operation.DailyLimit,
                operation.Consumed,
                operation.Remaining))
            .ToArray();

        return Ok(new UsageQuotaSummaryResponse(summary.ResetUtc, operations));
    }

    private static string GetDisplayName(UsageOperation operation) => operation switch
    {
        UsageOperation.ProductIdentification => "Product identification",
        UsageOperation.ProductKnowledge => "Product manual questions",
        UsageOperation.ProductAssistant => "Product assistant",
        _ => operation.ToString()
    };
}

public sealed record UsageQuotaSummaryResponse(
    DateTime ResetUtc,
    IReadOnlyList<UsageQuotaOperationResponse> Operations);

public sealed record UsageQuotaOperationResponse(
    string Operation,
    string DisplayName,
    int DailyLimit,
    int Consumed,
    int Remaining);