using ProductCare.Domain.Identity;

namespace ProductCare.Domain.Access
{
    public sealed class UserAccount
    {
        private UserAccount(
            Guid id,
            UserIdentity identity,
            string? email,
            string? displayName,
            UserAccountStatus status,
            UserPlan? plan,
            DateTime? accessExpiresUtc,
            DateTime createdUtc,
            DateTime updatedUtc)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("User account ID is required.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(identity);

            ValidateUtc(createdUtc, nameof(createdUtc));
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            if (accessExpiresUtc.HasValue)
            {
                ValidateUtc(accessExpiresUtc.Value, nameof(accessExpiresUtc));
            }

            if (updatedUtc < createdUtc)
            {
                throw new ArgumentException("Updated time cannot be earlier than created time.", nameof(updatedUtc));
            }

            Id = id;
            Identity = identity;
            Email = Normalize(email);
            DisplayName = Normalize(displayName);
            Status = status;
            Plan = plan;
            AccessExpiresUtc = accessExpiresUtc;
            CreatedUtc = createdUtc;
            UpdatedUtc = updatedUtc;
        }

        public Guid Id { get; }

        public UserIdentity Identity { get; }

        public string? Email { get; private set; }

        public string? DisplayName { get; private set; }

        public UserAccountStatus Status { get; private set; }

        public UserPlan? Plan { get; private set; }

        public DateTime? AccessExpiresUtc { get; private set; }

        public DateTime CreatedUtc { get; }

        public DateTime UpdatedUtc { get; private set; }

        public static UserAccount Create(
            UserIdentity identity,
            string? email,
            string? displayName,
            DateTime createdUtc)
        {
            ArgumentNullException.ThrowIfNull(identity);
            ValidateUtc(createdUtc, nameof(createdUtc));

            return new UserAccount(
                Guid.NewGuid(),
                identity,
                email,
                displayName,
                UserAccountStatus.Registered,
                null,
                null,
                createdUtc,
                createdUtc);
        }

        public static UserAccount Restore(
            Guid id,
            UserIdentity identity,
            string? email,
            string? displayName,
            UserAccountStatus status,
            UserPlan? plan,
            DateTime? accessExpiresUtc,
            DateTime createdUtc,
            DateTime updatedUtc)
        {
            return new UserAccount(
                id,
                identity,
                email,
                displayName,
                status,
                plan,
                NormalizePersistedUtc(accessExpiresUtc),
                NormalizePersistedUtc(createdUtc),
                NormalizePersistedUtc(updatedUtc));
        }

        public bool UpdateProfile(string? email, string? displayName, DateTime updatedUtc)
        {
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            string? normalizedEmail = Normalize(email);
            string? normalizedDisplayName = Normalize(displayName);

            if (Email == normalizedEmail && DisplayName == normalizedDisplayName)
            {
                return false;
            }

            Email = normalizedEmail;
            DisplayName = normalizedDisplayName;
            UpdatedUtc = updatedUtc;

            return true;
        }

        public void MarkPendingApproval(DateTime updatedUtc)
        {
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            UserAccountStatus effectiveStatus = GetEffectiveStatus(updatedUtc);

            if (effectiveStatus == UserAccountStatus.Active)
            {
                throw new InvalidOperationException("The account already has active access.");
            }

            if (effectiveStatus == UserAccountStatus.PendingApproval)
            {
                throw new InvalidOperationException("The account already has a pending access request.");
            }

            if (effectiveStatus == UserAccountStatus.Disabled)
            {
                throw new InvalidOperationException("A disabled account cannot request access.");
            }

            Status = UserAccountStatus.PendingApproval;
            Plan = null;
            AccessExpiresUtc = null;
            UpdatedUtc = updatedUtc;
        }

        public void ApproveDemoAccess(DateTime accessExpiresUtc, DateTime updatedUtc)
        {
            ValidateUtc(accessExpiresUtc, nameof(accessExpiresUtc));
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            if (Status != UserAccountStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only an account pending approval can be approved.");
            }

            if (accessExpiresUtc <= updatedUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(accessExpiresUtc), "Access expiration must be later than the approval time.");
            }

            Status = UserAccountStatus.Active;
            Plan = UserPlan.Demo;
            AccessExpiresUtc = accessExpiresUtc;
            UpdatedUtc = updatedUtc;
        }

        public void RejectAccess(DateTime updatedUtc)
        {
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            if (Status != UserAccountStatus.PendingApproval)
            {
                throw new InvalidOperationException("Only an account pending approval can be rejected.");
            }

            Status = UserAccountStatus.Rejected;
            Plan = null;
            AccessExpiresUtc = null;
            UpdatedUtc = updatedUtc;
        }

        public void Disable(DateTime updatedUtc)
        {
            ValidateUtc(updatedUtc, nameof(updatedUtc));

            Status = UserAccountStatus.Disabled;
            UpdatedUtc = updatedUtc;
        }

        public UserAccountStatus GetEffectiveStatus(DateTime utcNow)
        {
            ValidateUtc(utcNow, nameof(utcNow));

            if (Status == UserAccountStatus.Active &&
                AccessExpiresUtc.HasValue &&
                AccessExpiresUtc.Value <= utcNow)
            {
                return UserAccountStatus.Expired;
            }

            return Status;
        }

        public bool HasActiveAccess(DateTime utcNow)
        {
            return GetEffectiveStatus(utcNow) == UserAccountStatus.Active;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static DateTime NormalizePersistedUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                throw new ArgumentException("Persisted time must not be local.", nameof(value));
            }

            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value;
        }

        private static DateTime? NormalizePersistedUtc(DateTime? value)
        {
            return value.HasValue ? NormalizePersistedUtc(value.Value) : null;
        }

        private static void ValidateUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Time must be UTC.", parameterName);
            }
        }
    }
}