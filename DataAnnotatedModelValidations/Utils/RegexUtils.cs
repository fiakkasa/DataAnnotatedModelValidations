using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DataAnnotatedModelValidations.Utils;

[ExcludeFromCodeCoverage]
internal static partial class RegexUtils
{
    [ExcludeFromCodeCoverage]
    [GeneratedRegex("[\\[\\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    internal static partial Regex BracketsRegex();
}
