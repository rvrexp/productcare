namespace ProductCare.Application.ProductAssistant;

public interface IProductAssistantService
{
    Task<ProductAssistantResult> AskAsync(ProductAssistantQuery query, CancellationToken cancellationToken = default);

    Task<ProductAssistantResult> AskStreamingAsync(ProductAssistantQuery query, Func<string, CancellationToken, ValueTask> onAnswerDelta, CancellationToken cancellationToken = default);
}