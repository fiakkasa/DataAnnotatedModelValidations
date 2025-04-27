using DataAnnotatedModelValidations.Middleware;
using DataAnnotatedModelValidations.Models;
using System.Reflection;

namespace DataAnnotatedModelValidations.TypeInterceptors;

public sealed class ValidatorTypeInterceptor : TypeInterceptor
{
    private FieldMiddleware? _validatorMiddleware;

    private FieldMiddleware ValidatorMiddleware =>
        _validatorMiddleware ??= FieldClassMiddlewareFactory.Create<ValidatorMiddleware>();

    private static IBindableList<ObjectFieldDefinition>? ObjectTypeDefinitionFields(DefinitionBase? definition) =>
        definition switch
        {
            ObjectTypeDefinition { Fields.Count: > 0 } objectTypeDefinition
                when IsRootOperationType(objectTypeDefinition) => objectTypeDefinition.Fields,
            _ => default
        };

    private static bool IsRootOperationType(ObjectTypeDefinition objectTypeDefinition) =>
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

    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
    {
        if (ObjectTypeDefinitionFields(definition) is not { } fields)
        {
            return;
        }

        foreach (var field in fields)
        {
            var isValidatable = false;

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

                var customParameterAttributes = GetValidationAttributes(parameter);
                var shouldUseObjectValidator = ShouldUseObjectValidator(parameter);

                if (customParameterAttributes.Length > 0 || shouldUseObjectValidator)
                {
                    argument.ContextData[Consts.ArgumentValidationContextKey] =
                        new ArgumentValidationDefinition(
                            shouldUseObjectValidator,
                            customParameterAttributes
                        );
                    isValidatable = true;
                }
            }

            if (isValidatable)
            {
                field.ContextData[Consts.FieldValidationContextKey] = true;
                // add as first middleware to short circuit the pipeline
                field.MiddlewareDefinitions.Insert(0, new FieldMiddlewareDefinition(ValidatorMiddleware));
            }
        }
    }
}
