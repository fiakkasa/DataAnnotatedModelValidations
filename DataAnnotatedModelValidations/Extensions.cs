using System.Collections.Generic;

namespace DataAnnotatedModelValidations;

public static class Extensions
{
    public static IEnumerable<T> ToEnumerable<T>(this T value)
    {
        yield return value;
    }
}
