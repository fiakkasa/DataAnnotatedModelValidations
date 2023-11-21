#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.RegularExpressions;

namespace DataAnnotatedModelValidations.Utils;

internal static partial class RegexUtils
{
#if NET7_0_OR_GREATER
    [ExcludeFromCodeCoverage]
    [GeneratedRegex("[\\[\\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex BracketsRegex();
#endif

    private static readonly Regex _bracketsRegex =
#if NET7_0_OR_GREATER
        BracketsRegex();
#else
        new Regex("[\\[\\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
#endif

    internal static Regex GetBracketsRegex() => _bracketsRegex;
}
