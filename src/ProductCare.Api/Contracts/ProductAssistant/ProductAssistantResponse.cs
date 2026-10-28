namespace ProductCare.Api.Contracts.ProductAssistant;

public sealed record ProductAssistantResponse(
    string Answer,
    IReadOnlyList<ProductAssistantSourceResponse> Sources,
    IReadOnlyList<string> ToolsUsed);