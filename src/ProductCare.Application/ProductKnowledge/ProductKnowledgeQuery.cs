namespace ProductCare.Application.ProductKnowledge;

public sealed record ProductKnowledgeQuery(
    Guid ProductId,
    string Manufacturer,
    string ModelNumber,
    string Question);