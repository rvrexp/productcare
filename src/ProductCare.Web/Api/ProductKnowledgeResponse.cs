namespace ProductCare.Web.Api
{
    public sealed record ProductKnowledgeResponse(string Answer, IReadOnlyList<ProductKnowledgeCitationResponse> Citations);
}
