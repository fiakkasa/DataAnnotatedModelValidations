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
                    parameter.ParameterType.IsAssignableTo(Consts.ValidatableType)
                    || parameter
                        .ParameterType
                        .GetProperties()
                        .Any(prop => prop.GetCustomAttributes(Consts.ValidationType, true).Length > 0)
                )
                {
                    argument.ContextData[Consts.ArgumentValidationContextKey] = Array.Empty<object>();
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
