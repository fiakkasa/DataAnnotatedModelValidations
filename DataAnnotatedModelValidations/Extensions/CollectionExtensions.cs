namespace DataAnnotatedModelValidations.Extensions;

internal static class CollectionExtensions
{
    internal static IEnumerable<T> AsEnumerable<T>(this T value)
    {
        yield return value;
    }
}
