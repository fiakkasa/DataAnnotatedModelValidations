using DataAnnotatedModelValidations.Utils;
using System.Text.RegularExpressions;

namespace DataAnnotatedModelValidations.Tests.Utils;

public class RegexUtilsTests
{
    [Fact]
    public void Returns_Regular_Expression() =>
        Assert.IsAssignableFrom<Regex>(RegexUtils.BracketsRegularExpression);

    [Fact]
    public void Returns_Same_Instance_Of_Regular_Expression() =>
        Assert.Same(RegexUtils.BracketsRegularExpression, RegexUtils.BracketsRegularExpression);

    [Theory]
    [InlineData("[hello]", true)]
    [InlineData("[ hello ]", true)]
    [InlineData("[hello", true)]
    [InlineData("hello]", true)]
    [InlineData("[hello[world", true)]
    [InlineData("hello]world]", true)]
    [InlineData("hello[]world[]", true)]
    [InlineData("hello", false)]
    public void Matches_Target(string input, bool expected) =>
        Assert.Equal(expected, RegexUtils.BracketsRegularExpression.Matches(input).Count > 0);
}
