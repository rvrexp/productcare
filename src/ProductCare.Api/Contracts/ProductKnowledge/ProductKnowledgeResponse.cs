namespace ProductCare.Api.Contracts.ProductKnowledge
{
    public sealed record ProductKnowledgeResponse(string Answer, IReadOnlyList<ProductKnowledgeCitationResponse> Citations);
}
