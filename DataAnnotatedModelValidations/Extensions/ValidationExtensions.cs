using DataAnnotatedModelValidations.Models;

namespace DataAnnotatedModelValidations.Extensions;

internal static class ValidationExtensions
{
    private static bool ValidateAsValue(
        object item,
        string itemName,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults,
        ValidationAttribute[] attributes
    ) =>
        Validator.TryValidateValue(
            item,
            new(item, serviceProvider, default)
            {
                MemberName = itemName
            },
            validationResults,
            attributes
        );

    private static bool ValidateAsObject(
        object item,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults
    ) =>
        Validator.TryValidateObject(
            item,
            new(item, serviceProvider, default),
            validationResults,
            true
        );

    // validate top level and subsequently inner validation properties as to mimic the
    // short-circuiting behavior of a class with a top level validation attribute
    private static bool ValidateAsValueAndObject(
        object item,
        string itemName,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults,
        ValidationAttribute[] attributes
    ) =>
        ValidateAsValue(item, itemName, serviceProvider, validationResults, attributes)
        && ValidateAsObject(item, serviceProvider, validationResults);

    private static void AddInvalidCastExceptionValidationMessage(
        List<ValidationResult> validationResults,
        InvalidCastException ex
    ) =>
        validationResults.Add(
            new(
                ex switch
                {
                    { TargetSite.DeclaringType.Name: { Length: > 0 } name } =>
                        $"{ex.Message.TrimEnd('.')} for validation attribute {name}.",
                    _ => ex.Message
                }
            )
        );

    private static (bool success, bool? isValueValidation) ValidateItem(
        this IReadOnlyDictionary<string, object?> context,
        object item,
        string itemName,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults
    )
    {
        try
        {
            _ = context.TryGetValue(Consts.ArgumentValidationContextKey, out var argumentValidationDefinition);

            return argumentValidationDefinition switch
            {
                ArgumentValidationDefinition
                    {
                        UseObjectValidator: false,
                        ParameterAttributes: { Length: > 0 } attributes
                    } =>
                    (
                        success: ValidateAsValue(item, itemName, serviceProvider, validationResults, attributes),
                        isValueValidation: true
                    ),
                ArgumentValidationDefinition
                    {
                        UseObjectValidator: true,
                        ParameterAttributes: { Length: > 0 } attributes
                    } =>
                    (
                        success: ValidateAsValueAndObject(item, itemName, serviceProvider, validationResults, attributes),
                        isValueValidation: false
                    ),
                _ => (
                    success: ValidateAsObject(item, serviceProvider, validationResults),
                    isValueValidation: false
                )
            };
        }
        catch (InvalidCastException ex) when (ex.Source == "System.ComponentModel.Annotations")
        {
            AddInvalidCastExceptionValidationMessage(validationResults, ex);

            return (success: false, isValueValidation: default);
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

            var (success, isValueValidation) =
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
                        isValueValidation,
                        validationResult.ErrorMessage
                    );
                    continue;
                }

                foreach (var memberName in validationResult.MemberNames)
                {
                    context.ReportError(
                        argument,
                        contextPathList,
                        isValueValidation,
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
