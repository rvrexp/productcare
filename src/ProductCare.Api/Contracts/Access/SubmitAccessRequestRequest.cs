using System.ComponentModel.DataAnnotations;

namespace ProductCare.Api.Contracts.Access
{
    public sealed class SubmitAccessRequestRequest
    {
        [StringLength(200)]
        public string? Organization { get; init; }

        [Required]
        [StringLength(100)]
        public string Purpose { get; init; } = string.Empty;

        [StringLength(2000)]
        public string? Message { get; init; }
    }
}