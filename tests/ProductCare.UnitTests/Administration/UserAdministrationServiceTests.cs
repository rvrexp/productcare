using ProductCare.Application.Access;
using ProductCare.Application.Administration;
using ProductCare.Domain.Access;

namespace ProductCare.UnitTests.Administration;

public sealed class UserAdministrationServiceTests
{
    [Fact]
    public async Task SuspendAsync_SetsDisabled()
    {
        AdminUserRecord user = CreateUser(UserAccountStatus.Active, DateTime.UtcNow.AddDays(1));
        FakeUserAdministrationRepository repository = new(user);
        UserAdministrationService service = new(repository);

        AdminUserRecord? result = await service.SuspendAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(UserAccountStatus.Disabled, result.Status);
    }

    [Fact]
    public async Task RestoreAsync_WithUnexpiredAccess_SetsActive()
    {
        AdminUserRecord user = CreateUser(UserAccountStatus.Disabled, DateTime.UtcNow.AddDays(1));
        FakeUserAdministrationRepository repository = new(user);
        UserAdministrationService service = new(repository);

        AdminUserRecord? result = await service.RestoreAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(UserAccountStatus.Active, result.Status);
    }

    [Fact]
    public async Task RestoreAsync_WithExpiredAccess_SetsExpired()
    {
        AdminUserRecord user = CreateUser(UserAccountStatus.Disabled, DateTime.UtcNow.AddMinutes(-1));
        FakeUserAdministrationRepository repository = new(user);
        UserAdministrationService service = new(repository);

        AdminUserRecord? result = await service.RestoreAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(UserAccountStatus.Expired, result.Status);
    }

    [Fact]
    public async Task RestoreAsync_RegisteredAccountWithoutRequest_SetsRegistered()
    {
        AdminUserRecord user = CreateUser(UserAccountStatus.Disabled, null, null);
        FakeUserAdministrationRepository repository = new(user);
        UserAdministrationService service = new(repository);

        AdminUserRecord? result = await service.RestoreAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(UserAccountStatus.Registered, result.Status);
    }

    [Fact]
    public async Task RestoreAsync_AccountWithPendingRequest_SetsPendingApproval()
    {
        AdminUserRecord user = CreateUser(UserAccountStatus.Disabled, null, null);
        AccessRequestRecord pendingRequest = new(
            Guid.NewGuid(),
            user.Id,
            user.Email,
            user.DisplayName,
            "Contoso",
            "Portfolio / Interview Review",
            "Interview review",
            AccessRequestStatus.Pending,
            DateTime.UtcNow.AddHours(-1),
            null);

        FakeUserAdministrationRepository repository = new(user, [pendingRequest]);
        UserAdministrationService service = new(repository);

        AdminUserRecord? result = await service.RestoreAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(UserAccountStatus.PendingApproval, result.Status);
    }

    private static AdminUserRecord CreateUser(UserAccountStatus status, DateTime? expiresUtc, UserPlan? plan = UserPlan.Demo)
    {
        return new AdminUserRecord(
            Guid.NewGuid(),
            "tenant",
            "user",
            "Test User",
            "test@example.com",
            status,
            plan,
            expiresUtc,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);
    }

    private sealed class FakeUserAdministrationRepository : IUserAdministrationRepository
    {
        private AdminUserRecord _user;
        private readonly IReadOnlyList<AccessRequestRecord> _accessRequests;

        public FakeUserAdministrationRepository(AdminUserRecord user, IReadOnlyList<AccessRequestRecord>? accessRequests = null)
        {
            _user = user;
            _accessRequests = accessRequests ?? [];
        }

        public Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminUserRecord>>([_user]);
        }

        public Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminUserRecord?>(_user.Id == accountId ? _user : null);
        }

        public Task<AdminUserDetailsRecord?> GetDetailsAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            if (_user.Id != accountId)
            {
                return Task.FromResult<AdminUserDetailsRecord?>(null);
            }

            AdminUserDetailsRecord details = new(
                _user.Id,
                _user.TenantId,
                _user.UserId,
                _user.DisplayName,
                _user.Email,
                _user.Status,
                _user.Plan,
                _user.AccessExpiresUtc,
                _user.CreatedUtc,
                _user.UpdatedUtc,
                _accessRequests);

            return Task.FromResult<AdminUserDetailsRecord?>(details);
        }

        public Task<AdminUserRecord?> SetStatusAsync(Guid accountId, UserAccountStatus status, DateTime updatedUtc, CancellationToken cancellationToken = default)
        {
            if (_user.Id != accountId)
            {
                return Task.FromResult<AdminUserRecord?>(null);
            }

            _user = _user with
            {
                Status = status,
                UpdatedUtc = updatedUtc
            };

            return Task.FromResult<AdminUserRecord?>(_user);
        }
    }
}
