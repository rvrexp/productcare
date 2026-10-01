using ProductCare.Application.ProductKnowledge;

namespace ProductCare.UnitTests.ProductKnowledge;

public sealed class ProductKnowledgeKeyTests
{
    [Theory]
    [InlineData("Wayne", "CDU800", "wayne-cdu800")]
    [InlineData("ACME Corp.", "Model / 42", "acme-corp-model-42")]
    public void Create_NormalizesManufacturerAndModel(string manufacturer, string modelNumber, string expected)
    {
        string actual = ProductKnowledgeKey.Create(manufacturer, modelNumber);

        Assert.Equal(expected, actual);
    }
}