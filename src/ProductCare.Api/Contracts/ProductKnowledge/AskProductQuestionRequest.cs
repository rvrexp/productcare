using System.ComponentModel.DataAnnotations;

namespace ProductCare.Api.Contracts.ProductKnowledge
{
    public sealed record AskProductQuestionRequest([Required, StringLength(1000, MinimumLength = 3)] string Question);
}
