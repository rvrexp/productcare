using ProductCare.Domain.Identity;

namespace ProductCare.UnitTests.Identity;

public sealed class UserIdentityTests
{
    [Fact]
    public void Constructor_WithValidIdentity_NormalizesValues()
    {
        UserIdentity identity = new(" tenant-1 ", " user-1 ");

        Assert.Equal("tenant-1", identity.TenantId);
        Assert.Equal("user-1", identity.UserId);
    }

    [Fact]
    public void Constructor_WithoutTenantId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new UserIdentity(" ", "user-1"));
    }

    [Fact]
    public void Constructor_WithoutUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new UserIdentity("tenant-1", " "));
    }
}