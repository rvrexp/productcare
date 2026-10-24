using ProductCare.Application.Usage;

namespace ProductCare.Application.Common.Exceptions;

public sealed class QuotaExceededException : Exception
{
    public QuotaExceededException(UsageOperation operation, int dailyLimit, DateTime resetUtc)
        : base($"The daily usage limit for {operation} has been reached.")
    {
        Operation = operation;
        DailyLimit = dailyLimit;
        ResetUtc = resetUtc;
    }

    public UsageOperation Operation { get; }

    public int DailyLimit { get; }

    public DateTime ResetUtc { get; }
}