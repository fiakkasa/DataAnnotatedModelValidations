using DataAnnotatedModelValidations.Attributes;

namespace DataAnnotatedModelValidations;

internal static class Consts
{
    public const string ArgumentValidationContextKey = "ValidatableArgument";
    public const string FieldValidationContextKey = "ValidatableField";
    public static readonly Type IgnoreValidationType = typeof(IgnoreModelValidationAttribute);
    public static readonly Type ValidationType = typeof(ValidationAttribute);
    public static readonly Type ValidatableType = typeof(IValidatableObject);
}
