using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DataAnnotatedModelValidations.Utils;

internal static partial class RegexUtils
{
    [ExcludeFromCodeCoverage]
    [GeneratedRegex("[\\[\\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex BracketsRegex();

    private static readonly Regex _bracketsRegex = BracketsRegex();

    internal static Regex GetBracketsRegex() => _bracketsRegex;
}
