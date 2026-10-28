namespace ProductCare.Application.ProductAssistant;

public sealed record ProductAssistantQuery(
    Guid ProductId,
    string Manufacturer,
    string ModelNumber,
    string? ProductName,
    string Message)
{
    public string? AssistantConversationId { get; init; }

    public bool HasConversationHistory { get; init; }
}