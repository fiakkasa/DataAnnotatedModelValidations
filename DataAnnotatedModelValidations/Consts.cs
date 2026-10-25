using DataAnnotatedModelValidations.Attributes;

namespace DataAnnotatedModelValidations;

internal static class Consts
{
    public static readonly Type IgnoreValidationAttributeType = typeof(IgnoreModelValidationAttribute);
    public static readonly Type ValidationAttributeType = typeof(ValidationAttribute);
    public static readonly Type ValidatableObjectType = typeof(IValidatableObject);
}