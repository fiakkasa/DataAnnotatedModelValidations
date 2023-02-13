using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataAnnotatedModelValidations;

public class ValidatorTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase? definition)
    {
        if (definition is not ObjectTypeDefinition objectTypeDefinition)
            return;

        foreach (var field in objectTypeDefinition.Fields)
        {
            foreach (var argument in field.Arguments.Where(argument => argument is { Parameter: { } }))
            {
                if (
                    argument.Parameter!.IsDefined(typeof(IgnoreModelValidationAttribute), true)
                    || argument.Parameter!.ParameterType.IsDefined(typeof(IgnoreModelValidationAttribute), true)
                )
                {
                    argument.ContextData[nameof(IgnoreModelValidationAttribute)] = true;
                    continue;
                }

                if (argument.Parameter!.GetCustomAttributes(typeof(ValidationAttribute), true) is { Length: > 0 } attributes)
                    argument.ContextData[nameof(ValidationAttribute)] = attributes;
            }
        }
    }
}
