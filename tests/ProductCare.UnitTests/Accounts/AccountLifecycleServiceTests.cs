using ProductCare.Application.Accounts;

namespace ProductCare.UnitTests.Accounts;

public sealed class AccountLifecycleServiceTests
{
    [Fact]
    public async Task DeleteProductAsync_WhenOwnedProductExists_CleansResourcesThenDeletesRecord()
    {
        Guid productId = Guid.NewGuid();
        FakeAccountDataRepository repository = new([new AccountProductRecord(productId, "conversation-1")]);
        FakeProductResourceCleanupService cleanup = new();
        AccountLifecycleService service = new(repository, cleanup);

        bool deleted = await service.DeleteProductAsync("tenant", "user", productId, TestContext.Current.CancellationToken);

        Assert.True(deleted);
        Assert.Equal([productId], cleanup.DeletedProducts);
        Assert.True(repository.DeleteProductCalled);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductDoesNotExist_DoesNotCleanResources()
    {
        FakeAccountDataRepository repository = new([]);
        FakeProductResourceCleanupService cleanup = new();
        AccountLifecycleService service = new(repository, cleanup);

        bool deleted = await service.DeleteProductAsync("tenant", "user", Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Empty(cleanup.DeletedProducts);
        Assert.False(repository.DeleteProductCalled);
    }

    [Fact]
    public async Task PurgeAccountAsync_CleansProductsAndKeepsAccountRecordPathSeparate()
    {
        AccountProductRecord[] products = [new(Guid.NewGuid(), null)];
        FakeAccountDataRepository repository = new(products);
        FakeProductResourceCleanupService cleanup = new();
        AccountLifecycleService service = new(repository, cleanup);

        int count = await service.PurgeAccountAsync("tenant", "user", TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
        Assert.True(repository.PurgeOwnedDataCalled);
        Assert.False(repository.DeleteAccountDataCalled);
    }

    [Fact]
    public async Task DeleteAccountAsync_CleansAllProductsBeforeDeletingAccountData()
    {
        AccountProductRecord[] products =
        [
            new(Guid.NewGuid(), null),
            new(Guid.NewGuid(), "conversation-2")
        ];

        FakeAccountDataRepository repository = new(products);
        FakeProductResourceCleanupService cleanup = new();
        AccountLifecycleService service = new(repository, cleanup);

        int count = await service.DeleteAccountAsync("tenant", "user", TestContext.Current.CancellationToken);

        Assert.Equal(2, count);
        Assert.Equal(products.Select(product => product.Id), cleanup.DeletedProducts);
        Assert.True(repository.DeleteAccountDataCalled);
    }

    private sealed class FakeAccountDataRepository : IAccountDataRepository
    {
        private readonly List<AccountProductRecord> _products;

        public FakeAccountDataRepository(IEnumerable<AccountProductRecord> products)
        {
            _products = [.. products];
        }

        public bool DeleteProductCalled { get; private set; }

        public bool PurgeOwnedDataCalled { get; private set; }

        public bool DeleteAccountDataCalled { get; private set; }

        public Task<AccountProductRecord?> GetProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.SingleOrDefault(product => product.Id == productId));
        }

        public Task<IReadOnlyList<AccountProductRecord>> GetProductsAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccountProductRecord>>(_products);
        }

        public Task<bool> DeleteProductAsync(string tenantId, string userId, Guid productId, CancellationToken cancellationToken = default)
        {
            DeleteProductCalled = true;

            return Task.FromResult(_products.RemoveAll(product => product.Id == productId) > 0);
        }

        public Task PurgeOwnedDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            PurgeOwnedDataCalled = true;
            _products.Clear();

            return Task.CompletedTask;
        }

        public Task DeleteAccountDataAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
        {
            DeleteAccountDataCalled = true;
            _products.Clear();

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductResourceCleanupService : IProductResourceCleanupService
    {
        public List<Guid> DeletedProducts { get; } = [];

        public Task DeleteAsync(Guid productId, string? assistantConversationId, CancellationToken cancellationToken = default)
        {
            DeletedProducts.Add(productId);

            return Task.CompletedTask;
        }
    }
}