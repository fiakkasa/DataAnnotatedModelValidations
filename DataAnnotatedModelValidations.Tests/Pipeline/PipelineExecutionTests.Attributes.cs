using System;

namespace DataAnnotatedModelValidations.Tests.Pipeline;

public partial class PipelineExecutionTests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter)]
    public class AllStringPropertiesMustBeFilledAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
            value switch
            {
                { } obj when
                    obj.GetType().IsClass
                    && obj
                        .GetType()
                        .GetProperties()
                        .Where(propertyInfo => propertyInfo.PropertyType == typeof(string))
                        .Any(propertyInfo => propertyInfo.GetValue(obj, null) is not string { Length: > 0 }) =>
                    new("All string fields must be filled."),
                _ => ValidationResult.Success
            };
    }
}
