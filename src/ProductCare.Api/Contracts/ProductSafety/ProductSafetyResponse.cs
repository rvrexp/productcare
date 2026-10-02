namespace ProductCare.Api.Contracts.ProductSafety
{
    public sealed record ProductSafetyResponse(DateTime CheckedUtc, IReadOnlyList<ProductRecallResponse> Recalls);
}
