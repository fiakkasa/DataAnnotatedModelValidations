using Xunit;

namespace DataAnnotatedModelValidations.Tests;

public class ExtensionsTests
{
    [Fact(DisplayName = "ToEnumerable - Enumerate Value")]
    public void EnumerateValue() => Assert.Contains("test", "test".ToEnumerable());
}
