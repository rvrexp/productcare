namespace ProductCare.Application.Accounts;

public interface IAccountLifecycleService
{
    Task<bool> DeleteProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default);
    Task<int> PurgeAccountAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
    Task<int> DeleteAccountAsync(string tenantId, string userId, CancellationToken cancellationToken = default);
}
