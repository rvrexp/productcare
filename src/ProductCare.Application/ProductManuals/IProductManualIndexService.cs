namespace ProductCare.Application.ProductManuals;

public interface IProductManualIndexService
{
    Task<ProductManualStatus> GetStatusAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductManualStatus> UploadAsync(
        Guid productId,
        string manufacturer,
        string modelNumber,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ProductManualStatus> RetryAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}