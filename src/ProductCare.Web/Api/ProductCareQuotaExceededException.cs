namespace ProductCare.Web.Api;

public sealed class ProductCareQuotaExceededException : Exception
{
    public ProductCareQuotaExceededException(string? operation, int? dailyLimit, DateTime? retryAfterUtc)
        : base("The daily ProductCare AI usage limit has been reached.")
    {
        Operation = operation;
        DailyLimit = dailyLimit;
        RetryAfterUtc = retryAfterUtc;
    }

    public string? Operation { get; }

    public int? DailyLimit { get; }

    public DateTime? RetryAfterUtc { get; }
}