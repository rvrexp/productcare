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

    private static AdminUserRecord CreateUser(UserAccountStatus status, DateTime? expiresUtc)
    {
        return new AdminUserRecord(
            Guid.NewGuid(),
            "tenant",
            "user",
            "Test User",
            "test@example.com",
            status,
            UserPlan.Demo,
            expiresUtc,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);
    }

    private sealed class FakeUserAdministrationRepository : IUserAdministrationRepository
    {
        private AdminUserRecord _user;

        public FakeUserAdministrationRepository(AdminUserRecord user)
        {
            _user = user;
        }

        public Task<IReadOnlyList<AdminUserRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AdminUserRecord>>([_user]);
        }

        public Task<AdminUserRecord?> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AdminUserRecord?>(_user.Id == accountId ? _user : null);
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