namespace DataAnnotatedModelValidations.Utils;

internal static partial class RegexUtils
{
    internal static Regex BracketsRegularExpression { get; } = BracketsRegex();

    [ExcludeFromCodeCoverage]
    [GeneratedRegex(@"[\[\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex BracketsRegex();

    internal static Regex LeadingUppercaseRegularExpression { get; } = LeadingUppercaseRegex();

    /// <summary>
    /// Matches leading uppercase letters for camelCase conversion.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [GeneratedRegex(@"^[A-Z]+", RegexOptions.Compiled)]
    private static partial Regex LeadingUppercaseRegex();
}
