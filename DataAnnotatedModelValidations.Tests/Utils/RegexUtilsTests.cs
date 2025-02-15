using DataAnnotatedModelValidations.Utils;
using System.Text.RegularExpressions;

namespace DataAnnotatedModelValidations.Tests.Utils;

public class RegexUtilsTests
{
    [Fact]
    public void BracketsRegularExpression_Should_Return_An_Instance_Of_Regular_Expression() =>
        Assert.IsAssignableFrom<Regex>(RegexUtils.BracketsRegularExpression);

    [Fact]
    public void BracketsRegularExpression_Should_Return_The_Same_Instance_Of_Regular_Expression() =>
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
    public void BracketsRegularExpression_Should_Match_Target_Conditionally(string input, bool expected) =>
        Assert.Equal(expected, RegexUtils.BracketsRegularExpression.Matches(input).Count > 0);
}
