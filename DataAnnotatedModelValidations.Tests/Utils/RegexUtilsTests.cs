using System.Text.RegularExpressions;
using DataAnnotatedModelValidations.Utils;

namespace DataAnnotatedModelValidations.Tests;

public class RegexUtilsTests
{
    [Fact]
    public void Returns_Regular_Expression() =>
        Assert.IsAssignableFrom<Regex>(RegexUtils.GetBracketsRegex());

    [Fact]
    public void Returns_Same_Instance_Of_Regular_Expression() =>
        Assert.Same(RegexUtils.GetBracketsRegex(), RegexUtils.GetBracketsRegex());
}
