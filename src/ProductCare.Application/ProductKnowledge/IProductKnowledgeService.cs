namespace ProductCare.Application.ProductKnowledge;

public interface IProductKnowledgeService
{
    Task<ProductKnowledgeAnswer> AskAsync(ProductKnowledgeQuery query, CancellationToken cancellationToken = default);

    Task<ProductKnowledgeAnswer> AskStreamingAsync(ProductKnowledgeQuery query, Func<string, CancellationToken, ValueTask> onAnswerDelta, CancellationToken cancellationToken = default);
}