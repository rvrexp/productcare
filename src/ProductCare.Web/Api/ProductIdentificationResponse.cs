namespace ProductCare.Web.Api
{
    public sealed record ProductIdentificationResponse(
        string? Manufacturer,
        string? ModelNumber,
        string? ProductType,
        string? ProductName,
        string Confidence,
        IReadOnlyList<string> ObservedText);
}
