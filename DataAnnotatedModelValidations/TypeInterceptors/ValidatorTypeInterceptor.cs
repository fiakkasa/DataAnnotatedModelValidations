using DataAnnotatedModelValidations.Middleware;

namespace DataAnnotatedModelValidations.TypeInterceptors;

public sealed class ValidatorTypeInterceptor : TypeInterceptor
{
    private static IBindableList<ObjectFieldDefinition>? ObjectTypeDefinitionFields(DefinitionBase? definition) =>
        definition switch
        {
            ObjectTypeDefinition
            {
                Name: OperationTypeNames.Query
                or OperationTypeNames.Mutation
                or OperationTypeNames.Subscription,
                Fields.Count: > 0
            } objectTypeDefinition => objectTypeDefinition.Fields,
            _ => default
        };

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
                    || parameter.IsDefined(Consts.IgnoreValidationType, true)
                    || parameter.ParameterType.IsDefined(Consts.IgnoreValidationType, true)
                )
                {
                    continue;
                }

                var customAttributes = parameter.GetCustomAttributes(Consts.ValidationType, true);

                if (customAttributes.Length > 0)
                {
                    argument.ContextData[Consts.ArgumentValidationContextKey] = customAttributes;
                    isValidatable = true;
                }
                else if (
                    // implements IValidatableObject
                    parameter.ParameterType.IsAssignableTo(Consts.ValidatableType)
                    // annotated with ValidationType attribute
                    || parameter.ParameterType.GetCustomAttributes(Consts.ValidationType, true).Length > 0
                    // any property is annotated with a ValidationType attribute
                    || parameter
                        .ParameterType
                        .GetProperties()
                        .Any(prop => prop.GetCustomAttributes(Consts.ValidationType, true).Length > 0)
                )
                {
                    // Validator.TryValidateObject will be called for this argument and the argument
                    // will be validated according to the validation attributes or implementation of IValidatableObject
                    argument.ContextData[Consts.ArgumentValidationContextKey] = true;
                    isValidatable = true;
                }
            }

            if (isValidatable)
            {
                field.ContextData[Consts.FieldValidationContextKey] = true;
                field
                    .ToDescriptor(discoveryContext.DescriptorContext)
                    .Use<ValidatorMiddleware>();
            }
        }
    }
}
