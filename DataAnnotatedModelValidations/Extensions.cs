using System.Collections.Generic;

namespace DataAnnotatedModelValidations;

internal static class Extensions
{
    internal static IEnumerable<T> AsEnumerable<T>(this T value)
    {
        yield return value;
    }
}
