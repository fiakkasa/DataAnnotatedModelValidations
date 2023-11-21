using DataAnnotatedModelValidations.Attributes;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataAnnotatedModelValidations.TypeInterceptors;

public sealed class ValidatorTypeInterceptor : TypeInterceptor
{
    private readonly static Type _ignoreType = typeof(IgnoreModelValidationAttribute);
    private readonly static Type _validationType = typeof(ValidationAttribute);

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase? definition)
    {
        if (definition is not ObjectTypeDefinition { Fields: { Count: > 0 } fields })
            return;

        var collection = 
            fields
                .Where(field => field.Arguments.Count > 0)
                .SelectMany(field => field.Arguments)
                .Where(arg => arg is { Parameter: { } });

        foreach (var argument in collection)
        {
            if (
                argument.Parameter!.IsDefined(_ignoreType, true)
                || argument.Parameter!.ParameterType.IsDefined(_ignoreType, true)
            )
            {
                argument.ContextData[nameof(IgnoreModelValidationAttribute)] = true;
                continue;
            }

            if (argument.Parameter!.GetCustomAttributes(_validationType, true) is { Length: > 0 } attributes)
                argument.ContextData[nameof(ValidationAttribute)] = attributes;
        }
    }
}
