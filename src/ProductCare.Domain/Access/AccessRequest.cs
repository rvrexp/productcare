using ProductCare.Domain.Identity;

namespace ProductCare.Domain.Access
{
    public sealed class AccessRequest
    {
        private AccessRequest(Guid id, Guid userAccountId, string? organization, string purpose, string? message, AccessRequestStatus status, DateTime requestedUtc, DateTime? reviewedUtc, UserIdentity? reviewedBy)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Access request ID is required.", nameof(id));
            }

            if (userAccountId == Guid.Empty)
            {
                throw new ArgumentException("User account ID is required.", nameof(userAccountId));
            }

            if (string.IsNullOrWhiteSpace(purpose))
            {
                throw new ArgumentException("Access request purpose is required.", nameof(purpose));
            }

            if (!AccessRequestPurposes.IsSupported(purpose.Trim()))
            {
                throw new ArgumentException("The access request purpose is not supported.", nameof(purpose));
            }

            ValidateUtc(requestedUtc, nameof(requestedUtc));

            if (reviewedUtc.HasValue)
            {
                ValidateUtc(reviewedUtc.Value, nameof(reviewedUtc));
            }

            Id = id;
            UserAccountId = userAccountId;
            Organization = Normalize(organization);
            Purpose = purpose.Trim();
            Message = Normalize(message);
            Status = status;
            RequestedUtc = requestedUtc;
            ReviewedUtc = reviewedUtc;
            ReviewedBy = reviewedBy;
        }

        public Guid Id { get; }

        public Guid UserAccountId { get; }

        public string? Organization { get; }

        public string Purpose { get; }

        public string? Message { get; }

        public AccessRequestStatus Status { get; private set; }

        public DateTime RequestedUtc { get; }

        public DateTime? ReviewedUtc { get; private set; }

        public UserIdentity? ReviewedBy { get; private set; }

        public static AccessRequest Create(Guid userAccountId, string? organization, string purpose, string? message, DateTime requestedUtc)
        {
            return new AccessRequest(Guid.NewGuid(), userAccountId, organization, purpose, message, AccessRequestStatus.Pending, requestedUtc, null, null);
        }

        public static AccessRequest Restore(Guid id, Guid userAccountId, string? organization, string purpose, string? message, AccessRequestStatus status, DateTime requestedUtc, DateTime? reviewedUtc, UserIdentity? reviewedBy)
        {
            DateTime normalizedRequestedUtc = NormalizePersistedUtc(requestedUtc);
            DateTime? normalizedReviewedUtc = reviewedUtc.HasValue ? NormalizePersistedUtc(reviewedUtc.Value) : null;

            return new AccessRequest(id, userAccountId, organization, purpose, message, status, normalizedRequestedUtc, normalizedReviewedUtc, reviewedBy);
        }

        public void Approve(UserIdentity reviewer, DateTime reviewedUtc)
        {
            ArgumentNullException.ThrowIfNull(reviewer);
            ValidateReview(reviewedUtc);

            Status = AccessRequestStatus.Approved;
            ReviewedUtc = reviewedUtc;
            ReviewedBy = reviewer;
        }

        public void Reject(UserIdentity reviewer, DateTime reviewedUtc)
        {
            ArgumentNullException.ThrowIfNull(reviewer);
            ValidateReview(reviewedUtc);

            Status = AccessRequestStatus.Rejected;
            ReviewedUtc = reviewedUtc;
            ReviewedBy = reviewer;
        }

        private void ValidateReview(DateTime reviewedUtc)
        {
            ValidateUtc(reviewedUtc, nameof(reviewedUtc));

            if (Status != AccessRequestStatus.Pending)
            {
                throw new InvalidOperationException("Only a pending access request can be reviewed.");
            }

            if (reviewedUtc < RequestedUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(reviewedUtc), "Review time cannot be earlier than request time.");
            }
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ValidateUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Time must be UTC.", parameterName);
            }
        }

        private static DateTime NormalizePersistedUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException("Persisted time cannot be local time.");
            }

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}