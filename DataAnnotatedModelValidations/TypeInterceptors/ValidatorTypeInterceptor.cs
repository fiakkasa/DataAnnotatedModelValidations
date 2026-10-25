using DataAnnotatedModelValidations.Middleware;
using DataAnnotatedModelValidations.Models;
using System.Reflection;

namespace DataAnnotatedModelValidations.TypeInterceptors;

public sealed class ValidatorTypeInterceptor(bool bindUsingRootTypeFields = true) : TypeInterceptor
{
    private FieldMiddleware? _validatorMiddleware;

    private FieldMiddleware ValidatorMiddleware =>
        _validatorMiddleware ??= FieldClassMiddlewareFactory.Create<ValidatorMiddleware>();

    private static IBindableList<ObjectFieldConfiguration>? GetFields(bool useRootFields, TypeSystemConfiguration? configuration) =>
        configuration switch
        {
            ObjectTypeConfiguration { Fields: { Count: > 0 } fields } def
                when !useRootFields || IsRootOperationType(def) => fields,
            _ => default
        };

    private static bool IsRootOperationType(ObjectTypeConfiguration objectTypeDefinition) =>
        IsRootOperationTypeName(objectTypeDefinition.ExtendsType?.Name ?? objectTypeDefinition.Name);

    private static bool IsRootOperationTypeName(string? name) =>
        name is OperationTypeNames.Query
            or OperationTypeNames.Mutation
            or OperationTypeNames.Subscription;

    private static ValidationAttribute[] GetValidationAttributes(ParameterInfo parameter) =>
        parameter.GetCustomAttributes(Consts.ValidationAttributeType, true) switch
        {
            ValidationAttribute[] { Length: > 0 } parameterAttributes => parameterAttributes,
            _ => []
        };

    private static bool ShouldUseObjectValidator(ParameterInfo parameter) =>
        parameter.ParameterType.IsClass
        && (
            // implements IValidatableObject
            parameter.ParameterType.IsAssignableTo(Consts.ValidatableObjectType)
            // annotated with ValidationType attribute
            || parameter.ParameterType.GetCustomAttributes(Consts.ValidationAttributeType, true).Length > 0
            // any property is annotated with a ValidationType attribute
            || parameter
                .ParameterType
                .GetProperties()
                .Any(property => property.GetCustomAttributes(Consts.ValidationAttributeType, true).Length > 0)
        );

    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, TypeSystemConfiguration configuration)
    {
        if (GetFields(bindUsingRootTypeFields, configuration) is not { } fields)
        {
            return;
        }

        foreach (var field in fields)
        {
            if (SetArgumentValidationDefinitionToEligibleArguments(field) == 0)
            {
                continue;
            }

            // add as first middleware to short circuit the pipeline
            field.MiddlewareConfigurations.Insert(
                0,
                new FieldMiddlewareConfiguration(ValidatorMiddleware)
            );
        }
    }

    private static long SetArgumentValidationDefinitionToEligibleArguments(ObjectFieldConfiguration field)
    {
        var count = 0L;

        foreach (var argument in field.Arguments)
        {
            if (
                argument is not { Parameter: { } parameter }
                || parameter.IsDefined(Consts.IgnoreValidationAttributeType, true)
                || parameter.ParameterType.IsDefined(Consts.IgnoreValidationAttributeType, true)
            )
            {
                continue;
            }

            var validationAttributes = GetValidationAttributes(parameter);
            var useObjectValidator = ShouldUseObjectValidator(parameter);

            if (validationAttributes.Length > 0 || useObjectValidator)
            {
                argument.Features.Set(
                   new ArgumentValidationDefinition(useObjectValidator, validationAttributes)
               );
                count++;
            }
        }

        return count;
    }
}