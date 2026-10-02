namespace ProductCare.Web.Api
{
    public sealed record ProductSafetyResponse(DateTime CheckedUtc, IReadOnlyList<ProductRecallResponse> Recalls);
}
