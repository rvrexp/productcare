namespace ProductCare.Application.Accounts;

public interface IAccountDataRepository
{
    Task<AccountProductRecord?> GetProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountProductRecord>> GetProductsAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default);
    Task PurgeOwnedDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task DeleteAccountDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
}
