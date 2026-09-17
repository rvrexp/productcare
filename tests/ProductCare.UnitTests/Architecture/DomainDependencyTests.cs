using System.Reflection;

namespace ProductCare.UnitTests.Architecture;

public sealed class DomainDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var references = Assembly
            .Load("ProductCare.Domain")
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("ProductCare.Application", references);
        Assert.DoesNotContain("ProductCare.Infrastructure", references);
        Assert.DoesNotContain("ProductCare.AI", references);
        Assert.DoesNotContain("ProductCare.Api", references);
    }
}