using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.UnitTests.Access;

public sealed class UserAccountTests
{
    [Fact]
    public void Create_StartsAsRegistered()
    {
        DateTime now = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        UserIdentity identity = new("tenant-1", "user-1");

        UserAccount account = UserAccount.Create(identity, " user@example.com ", " Test User ", now);

        Assert.Equal(UserAccountStatus.Registered, account.Status);
        Assert.Null(account.Plan);
        Assert.Null(account.AccessExpiresUtc);
        Assert.Equal("user@example.com", account.Email);
        Assert.Equal("Test User", account.DisplayName);
    }

    [Fact]
    public void ApproveDemoAccess_WhenPending_ActivatesAccount()
    {
        DateTime now = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        DateTime expiresUtc = now.AddDays(14);
        UserAccount account = UserAccount.Create(new UserIdentity("tenant-1", "user-1"), null, null, now);

        account.MarkPendingApproval(now);
        account.ApproveDemoAccess(expiresUtc, now);

        Assert.Equal(UserAccountStatus.Active, account.Status);
        Assert.Equal(UserPlan.Demo, account.Plan);
        Assert.Equal(expiresUtc, account.AccessExpiresUtc);
        Assert.True(account.HasActiveAccess(now));
    }

    [Fact]
    public void ApproveDemoAccess_WhenNotPending_ThrowsInvalidOperationException()
    {
        DateTime now = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        UserAccount account = UserAccount.Create(new UserIdentity("tenant-1", "user-1"), null, null, now);

        Assert.Throws<InvalidOperationException>(() => account.ApproveDemoAccess(now.AddDays(14), now));
    }

    [Fact]
    public void HasActiveAccess_AfterExpiration_ReturnsFalse()
    {
        DateTime now = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        UserAccount account = UserAccount.Create(new UserIdentity("tenant-1", "user-1"), null, null, now);

        account.MarkPendingApproval(now);
        account.ApproveDemoAccess(now.AddDays(14), now);

        Assert.False(account.HasActiveAccess(now.AddDays(15)));
    }

    [Fact]
    public void RejectAccess_WhenPending_RejectsAccount()
    {
        DateTime now = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        UserAccount account = UserAccount.Create(new UserIdentity("tenant-1", "user-1"), null, null, now);

        account.MarkPendingApproval(now);
        account.RejectAccess(now);

        Assert.Equal(UserAccountStatus.Rejected, account.Status);
        Assert.Null(account.Plan);
        Assert.False(account.HasActiveAccess(now));
    }
}