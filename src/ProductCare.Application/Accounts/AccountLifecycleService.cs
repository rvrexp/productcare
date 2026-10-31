namespace ProductCare.Application.Accounts;

public sealed class AccountLifecycleService : IAccountLifecycleService
{
    private readonly IAccountDataRepository _repository;
    private readonly IProductResourceCleanupService _resourceCleanupService;

    public AccountLifecycleService(IAccountDataRepository repository, IProductResourceCleanupService resourceCleanupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _resourceCleanupService = resourceCleanupService ?? throw new ArgumentNullException(nameof(resourceCleanupService));
    }

    public async Task<bool> DeleteProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default)
    {
        ValidateIdentity(tenantId, userId);

        AccountProductRecord? product = await _repository.GetProductAsync(tenantId, userId, productId, cancellationToken);

        if (product is null)
        {
            return false;
        }

        await _resourceCleanupService.DeleteAsync(product.Id, product.AssistantConversationId, cancellationToken);

        return await _repository.DeleteProductAsync(tenantId, userId, product.Id, cancellationToken);
    }

    public async Task<int> PurgeAccountAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        ValidateIdentity(tenantId, userId);

        IReadOnlyList<AccountProductRecord> products = await CleanupProductsAsync(tenantId, userId, cancellationToken);
        int productCount = products.Count;

        await _repository.PurgeOwnedDataAsync(tenantId, userId, cancellationToken);

        return productCount;
    }

    public async Task<int> DeleteAccountAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        ValidateIdentity(tenantId, userId);

        IReadOnlyList<AccountProductRecord> products = await CleanupProductsAsync(tenantId, userId, cancellationToken);
        int productCount = products.Count;

        await _repository.DeleteAccountDataAsync(tenantId, userId, cancellationToken);

        return productCount;
    }

    private async Task<IReadOnlyList<AccountProductRecord>> CleanupProductsAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountProductRecord> products = await _repository.GetProductsAsync(tenantId, userId, cancellationToken);

        foreach (AccountProductRecord product in products)
        {
            await _resourceCleanupService.DeleteAsync(product.Id, product.AssistantConversationId, cancellationToken);
        }

        return products;
    }

    private static void ValidateIdentity(string tenantId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    }
}