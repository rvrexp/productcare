using ProductCare.Domain.Access;
using ProductCare.Domain.Identity;

namespace ProductCare.UnitTests.Access;

public sealed class AccessRequestTests
{
    [Fact]
    public void Create_StartsPending()
    {
        DateTime requestedUtc = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);

        AccessRequest request = AccessRequest.Create(Guid.NewGuid(), " Contoso ", " Technical Evaluation ", " Portfolio review ", requestedUtc);

        Assert.Equal(AccessRequestStatus.Pending, request.Status);
        Assert.Equal("Contoso", request.Organization);
        Assert.Equal("Technical Evaluation", request.Purpose);
        Assert.Equal("Portfolio review", request.Message);
        Assert.Null(request.ReviewedUtc);
        Assert.Null(request.ReviewedBy);
    }

    [Fact]
    public void Approve_WhenPending_RecordsReviewer()
    {
        DateTime requestedUtc = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        DateTime reviewedUtc = requestedUtc.AddMinutes(10);
        UserIdentity reviewer = new("tenant-1", "admin-1");
        AccessRequest request = AccessRequest.Create(Guid.NewGuid(), null, "Technical Evaluation", null, requestedUtc);

        request.Approve(reviewer, reviewedUtc);

        Assert.Equal(AccessRequestStatus.Approved, request.Status);
        Assert.Equal(reviewedUtc, request.ReviewedUtc);
        Assert.Equal(reviewer, request.ReviewedBy);
    }

    [Fact]
    public void Reject_WhenPending_RecordsReviewer()
    {
        DateTime requestedUtc = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        DateTime reviewedUtc = requestedUtc.AddMinutes(10);
        UserIdentity reviewer = new("tenant-1", "admin-1");
        AccessRequest request = AccessRequest.Create(Guid.NewGuid(), null, "Technical Evaluation", null, requestedUtc);

        request.Reject(reviewer, reviewedUtc);

        Assert.Equal(AccessRequestStatus.Rejected, request.Status);
        Assert.Equal(reviewer, request.ReviewedBy);
    }

    [Fact]
    public void Approve_AfterRequestWasRejected_ThrowsInvalidOperationException()
    {
        DateTime requestedUtc = new(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
        UserIdentity reviewer = new("tenant-1", "admin-1");
        AccessRequest request = AccessRequest.Create(Guid.NewGuid(), null, "Technical Evaluation", null, requestedUtc);

        request.Reject(reviewer, requestedUtc.AddMinutes(5));

        Assert.Throws<InvalidOperationException>(() => request.Approve(reviewer, requestedUtc.AddMinutes(10)));
    }
    [Fact]
    public void Create_WithUnsupportedPurpose_ThrowsArgumentException()
    {
        DateTime requestedUtc = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => AccessRequest.Create(
            Guid.NewGuid(),
            null,
            "Something Else",
            null,
            requestedUtc));
    }
}