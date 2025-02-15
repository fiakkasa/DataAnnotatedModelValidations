using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void AsEnumerable_Should_Enumerate_Once() =>
        Assert.Single("test".AsEnumerable().Take(10));
}
