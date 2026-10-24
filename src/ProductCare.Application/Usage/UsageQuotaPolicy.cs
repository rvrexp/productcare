namespace ProductCare.Application.Usage;

public sealed class UsageQuotaPolicy
{
    public UsageQuotaPolicy(int productIdentificationDailyLimit, int productKnowledgeDailyLimit, int productAssistantDailyLimit)
    {
        ProductIdentificationDailyLimit = ValidateLimit(productIdentificationDailyLimit, nameof(productIdentificationDailyLimit));
        ProductKnowledgeDailyLimit = ValidateLimit(productKnowledgeDailyLimit, nameof(productKnowledgeDailyLimit));
        ProductAssistantDailyLimit = ValidateLimit(productAssistantDailyLimit, nameof(productAssistantDailyLimit));
    }

    public int ProductIdentificationDailyLimit { get; }

    public int ProductKnowledgeDailyLimit { get; }

    public int ProductAssistantDailyLimit { get; }

    public int GetDailyLimit(UsageOperation operation) => operation switch
    {
        UsageOperation.ProductIdentification => ProductIdentificationDailyLimit,
        UsageOperation.ProductKnowledge => ProductKnowledgeDailyLimit,
        UsageOperation.ProductAssistant => ProductAssistantDailyLimit,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported usage operation.")
    };

    private static int ValidateLimit(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Daily usage limits must be greater than zero.");
        }

        return value;
    }
}