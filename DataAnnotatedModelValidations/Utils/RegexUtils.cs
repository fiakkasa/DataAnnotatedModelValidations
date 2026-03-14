namespace DataAnnotatedModelValidations.Utils;

internal static partial class RegexUtils
{
    internal static Regex BracketsRegularExpression { get; } = BracketsRegex();

    [ExcludeFromCodeCoverage]
    [GeneratedRegex(@"[\[\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex BracketsRegex();
}
