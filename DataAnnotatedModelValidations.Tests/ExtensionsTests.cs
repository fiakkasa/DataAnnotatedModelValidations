namespace DataAnnotatedModelValidations.Tests;

public class ExtensionsTests
{
    [Fact(DisplayName = "AsEnumerable - Enumerate Value")]
    public void EnumerateValue() => Assert.Contains("test", "test".AsEnumerable());
}
