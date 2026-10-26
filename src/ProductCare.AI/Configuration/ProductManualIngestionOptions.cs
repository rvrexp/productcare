namespace ProductCare.AI.Configuration;

public sealed class ProductManualIngestionOptions
{
    public const string SectionName = "ProductManualIngestion";

    public string StorageServiceUri { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    public string IndexerName { get; set; } = string.Empty;
}