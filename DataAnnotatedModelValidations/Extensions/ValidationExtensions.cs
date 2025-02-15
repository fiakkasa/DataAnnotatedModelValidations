namespace DataAnnotatedModelValidations.Extensions;

internal static class ValidationExtensions
{
    private static (bool success, bool? valueValidation) ValidateItem(
        this IReadOnlyDictionary<string, object?> context,
        object item,
        string itemName,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults
    )
    {
        try
        {
            _ = context.TryGetValue(Consts.ArgumentValidationContextKey, out var attrs);

            return attrs switch
            {
                IEnumerable<ValidationAttribute> attributes =>
                (
                    success: Validator.TryValidateValue(
                        item,
                        new(item, serviceProvider, default)
                        {
                            MemberName = itemName
                        },
                        validationResults,
                        attributes
                    ),
                    valueValidation: true
                ),
                _ =>
                (
                    success: Validator.TryValidateObject(
                        item,
                        new(item, serviceProvider, default),
                        validationResults,
                        true
                    ),
                    valueValidation: false
                )
            };
        }
        catch (InvalidCastException ex) when (ex.Source == "System.ComponentModel.Annotations")
        {
            validationResults.Add(
                new(
                    ex switch
                    {
                        { TargetSite.DeclaringType.Name: { Length: > 0 } name } =>
                            $"{ex.Message.TrimEnd('.')} for validation attribute {name}",
                        _ => ex.Message
                    }
                )
            );

            return (success: false, valueValidation: default);
        }
    }

    private static Action<IInputField> ValidateAndReport(IMiddlewareContext context) =>
        argument =>
        {
            if (context.ArgumentValue<object>(argument.Name) is not { } item)
            {
                return;
            }

            var validationResults = new List<ValidationResult>();

            var (success, valueValidation) =
                argument.ContextData.ValidateItem(
                    item,
                    argument.Name,
                    context.Services,
                    validationResults
                );

            if (success)
            {
                validationResults.Clear();
                return;
            }

            var contextPathList =
                context
                    .Path
                    .ToList()
                    .OfType<string>()
                    .ToList();

            foreach (var validationResult in validationResults)
            {
                if (!validationResult.MemberNames.Any())
                {
                    context.ReportError(
                        argument,
                        contextPathList,
                        valueValidation,
                        validationResult.ErrorMessage
                    );
                    continue;
                }

                foreach (var memberName in validationResult.MemberNames)
                {
                    context.ReportError(
                        argument,
                        contextPathList,
                        valueValidation,
                        validationResult.ErrorMessage,
                        memberName
                    );
                }
            }

            validationResults.Clear();
        };

    internal static void ValidateInputs(this IMiddlewareContext context)
    {
        if (
            !context.Selection.Field.ContextData.ContainsKey(Consts.FieldValidationContextKey)
            || context.Selection.Field.Arguments is not { Count: > 0 } arguments
        )
        {
            return;
        }

        arguments
            .AsParallel()
            .Where(argument =>
                argument?.ContextData.ContainsKey(Consts.ArgumentValidationContextKey) == true
            )
            .ForAll(ValidateAndReport(context));
    }
}
