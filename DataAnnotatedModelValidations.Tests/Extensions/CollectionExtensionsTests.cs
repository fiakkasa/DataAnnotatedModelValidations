using System.Linq;
using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests;

public class CollectionExtensionsTests
{
    [Fact(DisplayName = "AsEnumerable - Enumerate Value")]
    public void EnumerateValue() => Assert.Contains("test", "test".AsEnumerable());
}
