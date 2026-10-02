namespace ProductCare.Api.Contracts.ProductSafety
{
    public sealed record ProductRecallResponse(
        string RecallNumber,
        string Title,
        DateTime? RecallDate,
        string? Description,
        string? Hazard,
        string? Remedy,
        string? ConsumerContact,
        string? OfficialUrl);
}
