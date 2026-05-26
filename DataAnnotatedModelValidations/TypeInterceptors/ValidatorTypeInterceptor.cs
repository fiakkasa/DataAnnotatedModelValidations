using DataAnnotatedModelValidations.Middleware;
using DataAnnotatedModelValidations.Models;
using System.Reflection;

namespace DataAnnotatedModelValidations.TypeInterceptors;

public sealed class ValidatorTypeInterceptor(bool restrictToRootTypes = true) : TypeInterceptor
{
    private FieldMiddleware? _validatorMiddleware;

    private FieldMiddleware ValidatorMiddleware =>
        _validatorMiddleware ??= FieldClassMiddlewareFactory.Create<ValidatorMiddleware>();

    private IBindableList<ObjectFieldDefinition>? GetFields(DefinitionBase? definition) =>
        (restrictToRootTypes, definition) switch
        {
            (true, ObjectTypeDefinition { Fields: { Count: > 0 } fields } def)
                when IsRootOperationType(def) => fields,
            (false, ObjectTypeDefinition { Fields: { Count: > 0 } fields }) => fields,
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
        if (GetFields(definition) is not { } fields)
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
