using System.Linq;
using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests;

public class CollectionExtensionsTests
{
    [Fact]
    public void AsEnumerable_Enumerates_A_Value_Once() => 
        Assert.Single("test".AsEnumerable().Take(10));
}
