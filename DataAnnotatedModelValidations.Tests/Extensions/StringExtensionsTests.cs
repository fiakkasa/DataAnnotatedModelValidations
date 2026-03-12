using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Tests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("HelloWorld[0]", "helloWorld[0]")] // Should leave brackets and numbers alone

    // Match the tests found in HotChocolate - DefaultNamingConventionsTests.cs
    [InlineData("f", "f")]
    [InlineData("IOFile", "ioFile")]
    [InlineData("FooBar", "fooBar")]
    [InlineData("FOO1Bar", "foo1Bar")]
    [InlineData("FOO_Bar", "foo_Bar")]
    [InlineData("FOO", "foo")]
    [InlineData("FOo", "fOo")]
    [InlineData("FOOBarBaz", "fooBarBaz")]
    [InlineData("FoO", "foO")]
    [InlineData("F", "f")]
    public void GetCamelCaseName(string input, string expected) =>
        Assert.Equal(expected, input.Camelize());
}
