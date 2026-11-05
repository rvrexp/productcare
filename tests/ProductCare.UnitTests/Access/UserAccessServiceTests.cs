using ProductCare.Application.Access;
using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.UnitTests.Access;

public sealed class UserAccessServiceTests
{
    [Fact]
    public async Task GetOrRegisterStatusAsync_FirstAuthenticatedAccess_CreatesRegisteredAccount()
    {
        DateTime now = new(2026, 11, 4, 17, 0, 0, DateTimeKind.Utc);
        FakeUserAccessRepository repository = new();
        UserAccessService service = new(repository, new FixedTimeProvider(new DateTimeOffset(now)));
        AccessUser user = new(new UserIdentity("tenant", "user"), "test@example.com", "Test User");

        UserAccessRecord result = await service.GetOrRegisterStatusAsync(user, TestContext.Current.CancellationToken);

        UserAccount persistedAccount = repository.Account ?? throw new InvalidOperationException("The registered account was not persisted.");

        Assert.NotNull(result.UserAccountId);
        Assert.Equal(persistedAccount.Id, result.UserAccountId);
        Assert.Equal(UserAccountStatus.Registered, result.Status);
        Assert.Equal("test@example.com", persistedAccount.Email);
        Assert.Equal("Test User", persistedAccount.DisplayName);
        Assert.Equal(now, persistedAccount.CreatedUtc);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task GetOrRegisterStatusAsync_ExistingAccount_RefreshesProfileFromAuthenticatedClaims()
    {
        DateTime createdUtc = new(2026, 11, 3, 17, 0, 0, DateTimeKind.Utc);
        DateTime now = new(2026, 11, 4, 17, 0, 0, DateTimeKind.Utc);
        UserIdentity identity = new("tenant", "user");
        UserAccount account = UserAccount.Create(identity, "old@example.com", "Old Name", createdUtc);
        FakeUserAccessRepository repository = new(account);
        UserAccessService service = new(repository, new FixedTimeProvider(new DateTimeOffset(now)));
        AccessUser user = new(identity, "new@example.com", "New Name");

        UserAccessRecord result = await service.GetOrRegisterStatusAsync(user, TestContext.Current.CancellationToken);

        UserAccount persistedAccount = repository.Account ?? throw new InvalidOperationException("The existing account was not persisted.");

        Assert.Equal(UserAccountStatus.Registered, result.Status);
        Assert.Equal("new@example.com", persistedAccount.Email);
        Assert.Equal("New Name", persistedAccount.DisplayName);
        Assert.Equal(now, persistedAccount.UpdatedUtc);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task GetStatusAsync_MissingAccount_RemainsReadOnly()
    {
        FakeUserAccessRepository repository = new();
        UserAccessService service = new(repository, TimeProvider.System);
        UserIdentity identity = new("tenant", "user");

        UserAccessRecord result = await service.GetStatusAsync(identity, TestContext.Current.CancellationToken);

        Assert.Null(repository.Account);
        Assert.Null(result.UserAccountId);
        Assert.Equal(UserAccountStatus.Registered, result.Status);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class FakeUserAccessRepository : IUserAccessRepository
    {
        private readonly List<AccessRequest> _requests = [];

        public FakeUserAccessRepository(UserAccount? account = null)
        {
            Account = account;
        }

        public UserAccount? Account { get; private set; }

        public int SaveChangesCount { get; private set; }

        public Task<UserAccount?> GetAccountAsync(UserIdentity identity, CancellationToken cancellationToken = default)
        {
            UserAccount? account = Account is not null &&
                                   Account.Identity.TenantId == identity.TenantId &&
                                   Account.Identity.UserId == identity.UserId
                ? Account
                : null;

            return Task.FromResult(account);
        }

        public Task<UserAccount?> GetAccountAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Account?.Id == userAccountId ? Account : null);
        }

        public Task<AccessRequest?> GetRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_requests.SingleOrDefault(request => request.Id == requestId));
        }

        public Task<bool> HasPendingRequestAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_requests.Any(request => request.UserAccountId == userAccountId && request.Status == AccessRequestStatus.Pending));
        }

        public Task<DateTime?> GetLatestRequestDateAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            DateTime? requestedUtc = _requests
                .Where(request => request.UserAccountId == userAccountId)
                .OrderByDescending(request => request.RequestedUtc)
                .Select(request => (DateTime?)request.RequestedUtc)
                .FirstOrDefault();

            return Task.FromResult(requestedUtc);
        }

        public Task<IReadOnlyList<AccessRequestRecord>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccessRequestRecord>>([]);
        }

        public void Add(UserAccount account)
        {
            Account = account;
        }

        public void Add(AccessRequest request)
        {
            _requests.Add(request);
        }

        public Task UpdateAsync(UserAccount account, CancellationToken cancellationToken = default)
        {
            Account = account;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }
}