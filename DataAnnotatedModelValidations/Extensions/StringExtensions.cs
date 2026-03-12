using DataAnnotatedModelValidations.Utils;

namespace DataAnnotatedModelValidations.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Formats a field name to camelCase.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    ///   <item><c>FOOBar</c> → <c>fooBar</c></item>
    ///   <item><c>FOO1Ar</c> → <c>foo1Ar</c></item>
    ///   <item><c>FOO_Ar</c> → <c>foo_Ar</c></item>
    ///   <item><c>ID</c> → <c>id</c></item>
    /// </list>
    /// </remarks>
    internal static string Camelize(this string fieldName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        // If the first character is already lowercase, return as is.
        if (char.IsLower(fieldName[0]))
        {
            return fieldName;
        }

        return RegexUtils.LeadingUppercaseRegularExpression.Replace(fieldName, match =>
        {
            var upper = match.Value;
            var nextIndex = match.Length;

            // If only one uppercase char, or followed by non-letter, lowercase all
            if (upper.Length == 1 || nextIndex >= fieldName.Length || !char.IsLower(fieldName[nextIndex]))
                return upper.ToLowerInvariant();

            // Multiple uppercase chars followed by lowercase: keep last uppercase
            // (it starts the next camelCase word)
            return upper[..^1].ToLowerInvariant() + upper[^1];
        });
    }
}
