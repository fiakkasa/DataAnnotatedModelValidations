namespace DataAnnotatedModelValidations.Models;

internal record ArgumentValidationDefinition(
    bool UseObjectValidator,
    ValidationAttribute[] ParameterAttributes
);
