using ProductCare.Application.Common.Exceptions;
using ProductCare.Application.Usage;
using ProductCare.Domain.Identity;
using Xunit;

namespace ProductCare.UnitTests.Usage;

public sealed class UsageQuotaServiceTests
{
    [Fact]
    public async Task ConsumeAsync_WhenBelowLimit_IncrementsUsageAndReturnsRemainingQuota()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 15, 30, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();
        UsageQuotaService service = CreateService(repository, timeProvider);

        UserIdentity identity = new("tenant-1", "user-1");

        UsageQuotaConsumption result = await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);

        Assert.Equal(UsageOperation.ProductIdentification, result.Operation);
        Assert.Equal(20, result.DailyLimit);
        Assert.Equal(1, result.Consumed);
        Assert.Equal(19, result.Remaining);
        Assert.Equal(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), result.ResetUtc);
    }

    [Fact]
    public async Task ConsumeAsync_WhenDailyLimitReached_ThrowsQuotaExceededException()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 15, 30, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();

        UsageQuotaPolicy policy = new(1, 50, 50);
        UsageQuotaService service = new(repository, policy, timeProvider);

        UserIdentity identity = new("tenant-1", "user-1");

        await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);

        QuotaExceededException exception = await Assert.ThrowsAsync<QuotaExceededException>(
            () => service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken));

        Assert.Equal(UsageOperation.ProductIdentification, exception.Operation);
        Assert.Equal(1, exception.DailyLimit);
        Assert.Equal(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), exception.ResetUtc);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenNothingHasBeenUsed_ReturnsFullQuotaWithoutCreatingUsage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 15, 30, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();
        UsageQuotaService service = CreateService(repository, timeProvider);

        UserIdentity identity = new("tenant-1", "user-1");

        UsageQuotaSummary firstSummary = await service.GetSummaryAsync(identity, cancellationToken);
        UsageQuotaSummary secondSummary = await service.GetSummaryAsync(identity, cancellationToken);

        Assert.Collection(
            firstSummary.Operations,
            operation =>
            {
                Assert.Equal(UsageOperation.ProductIdentification, operation.Operation);
                Assert.Equal(20, operation.DailyLimit);
                Assert.Equal(0, operation.Consumed);
                Assert.Equal(20, operation.Remaining);
            },
            operation =>
            {
                Assert.Equal(UsageOperation.ProductKnowledge, operation.Operation);
                Assert.Equal(50, operation.DailyLimit);
                Assert.Equal(0, operation.Consumed);
                Assert.Equal(50, operation.Remaining);
            },
            operation =>
            {
                Assert.Equal(UsageOperation.ProductAssistant, operation.Operation);
                Assert.Equal(50, operation.DailyLimit);
                Assert.Equal(0, operation.Consumed);
                Assert.Equal(50, operation.Remaining);
            });

        Assert.All(secondSummary.Operations, operation => Assert.Equal(0, operation.Consumed));

        UsageQuotaConsumption consumption = await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);

        Assert.Equal(1, consumption.Consumed);
    }

    [Fact]
    public async Task GetSummaryAsync_AfterConsumption_ReturnsCurrentRemainingQuota()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 15, 30, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();
        UsageQuotaService service = CreateService(repository, timeProvider);

        UserIdentity identity = new("tenant-1", "user-1");

        await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);
        await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);
        await service.ConsumeAsync(identity, UsageOperation.ProductKnowledge, cancellationToken);

        UsageQuotaSummary summary = await service.GetSummaryAsync(identity, cancellationToken);

        UsageQuotaOperationSummary identification = summary.Operations.Single(operation => operation.Operation == UsageOperation.ProductIdentification);
        UsageQuotaOperationSummary knowledge = summary.Operations.Single(operation => operation.Operation == UsageOperation.ProductKnowledge);
        UsageQuotaOperationSummary assistant = summary.Operations.Single(operation => operation.Operation == UsageOperation.ProductAssistant);

        Assert.Equal(2, identification.Consumed);
        Assert.Equal(18, identification.Remaining);

        Assert.Equal(1, knowledge.Consumed);
        Assert.Equal(49, knowledge.Remaining);

        Assert.Equal(0, assistant.Consumed);
        Assert.Equal(50, assistant.Remaining);
    }

    [Fact]
    public async Task Usage_IsScopedToIndividualUsers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 15, 30, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();
        UsageQuotaService service = CreateService(repository, timeProvider);

        UserIdentity firstUser = new("tenant-1", "user-1");
        UserIdentity secondUser = new("tenant-1", "user-2");

        await service.ConsumeAsync(firstUser, UsageOperation.ProductAssistant, cancellationToken);

        UsageQuotaSummary firstSummary = await service.GetSummaryAsync(firstUser, cancellationToken);
        UsageQuotaSummary secondSummary = await service.GetSummaryAsync(secondUser, cancellationToken);

        Assert.Equal(1, firstSummary.Operations.Single(operation => operation.Operation == UsageOperation.ProductAssistant).Consumed);
        Assert.Equal(0, secondSummary.Operations.Single(operation => operation.Operation == UsageOperation.ProductAssistant).Consumed);
    }

    [Fact]
    public async Task Usage_ResetsOnNextUtcDay()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MutableTimeProvider timeProvider = new(new DateTime(2026, 10, 24, 23, 59, 0, DateTimeKind.Utc));
        FakeUsageQuotaRepository repository = new();
        UsageQuotaService service = CreateService(repository, timeProvider);

        UserIdentity identity = new("tenant-1", "user-1");

        await service.ConsumeAsync(identity, UsageOperation.ProductIdentification, cancellationToken);

        timeProvider.SetUtcNow(new DateTime(2026, 10, 25, 0, 1, 0, DateTimeKind.Utc));

        UsageQuotaSummary summary = await service.GetSummaryAsync(identity, cancellationToken);

        UsageQuotaOperationSummary identification = summary.Operations.Single(operation => operation.Operation == UsageOperation.ProductIdentification);

        Assert.Equal(0, identification.Consumed);
        Assert.Equal(20, identification.Remaining);
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), summary.ResetUtc);
    }

    private static UsageQuotaService CreateService(FakeUsageQuotaRepository repository, TimeProvider timeProvider)
    {
        UsageQuotaPolicy policy = new(20, 50, 50);

        return new UsageQuotaService(repository, policy, timeProvider);
    }

    private sealed class FakeUsageQuotaRepository : IUsageQuotaRepository
    {
        private readonly Dictionary<(string TenantId, string UserId, UsageOperation Operation, DateOnly Date), int> _usage = [];

        public Task<int?> TryConsumeAsync(
            UserIdentity identity,
            UsageOperation operation,
            DateOnly usageDateUtc,
            int dailyLimit,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string TenantId, string UserId, UsageOperation Operation, DateOnly Date) key =
                (identity.TenantId, identity.UserId, operation, usageDateUtc);

            _usage.TryGetValue(key, out int current);

            if (current >= dailyLimit)
            {
                return Task.FromResult<int?>(null);
            }

            int updated = current + 1;
            _usage[key] = updated;

            return Task.FromResult<int?>(updated);
        }

        public Task<IReadOnlyDictionary<UsageOperation, int>> GetUsageAsync(
            UserIdentity identity,
            DateOnly usageDateUtc,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<UsageOperation, int> result = _usage
                .Where(item =>
                    item.Key.TenantId == identity.TenantId &&
                    item.Key.UserId == identity.UserId &&
                    item.Key.Date == usageDateUtc)
                .ToDictionary(item => item.Key.Operation, item => item.Value);

            return Task.FromResult<IReadOnlyDictionary<UsageOperation, int>>(result);
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow);
        }
    }
}