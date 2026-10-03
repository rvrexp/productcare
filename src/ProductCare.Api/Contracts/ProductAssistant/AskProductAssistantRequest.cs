using System.ComponentModel.DataAnnotations;

namespace ProductCare.Api.Contracts.ProductAssistant
{
    public sealed record AskProductAssistantRequest([Required, StringLength(2000, MinimumLength = 1)] string Message);
}
