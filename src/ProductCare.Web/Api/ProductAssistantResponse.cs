namespace ProductCare.Web.Api
{
    public sealed record ProductAssistantResponse(string Answer, IReadOnlyList<ProductAssistantSourceResponse> Sources, IReadOnlyList<string> ToolsUsed);
}
