using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Resolvers;
using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IInputField = HotChocolate.Types.IInputField;
using HotChocolate.Execution;

namespace DataAnnotatedModelValidations;

public partial class ValidatorMiddleware
{
    private readonly FieldDelegate _next;

    private static readonly Regex _bracketsRegex = BracketsRegex();

    public ValidatorMiddleware(FieldDelegate next) => _next = next;

    private static string MemberNameToNameString(string memberName) =>
        new(_bracketsRegex.Replace(memberName.Camelize(), "_"));

    private static NamePathSegment GenerateArgumentPath(
        string name,
        string? memberName,
        List<string> contextPath,
        bool? valueValidation
    ) =>
        contextPath
            .Skip(1)
            .Concat(
                (memberName?.Trim(), valueValidation) switch
                {
                    ({ Length: > 0 } trimmedMemberName, true) =>
                        trimmedMemberName
                            .Split(':')
                            .Select(MemberNameToNameString),
                    ({ Length: > 0 } trimmedMemberName, _) =>
                        trimmedMemberName
                            .Split(':')
                            .Select(MemberNameToNameString)
                            .Prepend(name),
                    _ => name.ToEnumerable()
                }
            )
            .Aggregate(PathFactory.Instance.New(contextPath[0]), PathFactory.Instance.Append);

    private static void ReportError(
        IMiddlewareContext context,
        IInputField argument,
        List<string> contextPathList,
        bool? valueValidation,
        string? message = default,
        string? memberName = default
    ) =>
        context.ReportError(
            ErrorBuilder.New()
                .SetMessage(message ?? "Unspecified Error")
                .SetCode("DAMV-400")
                .SetPath(GenerateArgumentPath(argument.Name, memberName, contextPathList, valueValidation))
                .SetExtension("field", argument.Coordinate.FieldName)
                .SetExtension("type", argument.Coordinate.TypeName)
                .SetExtension("specifiedBy", "http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type")
                .Build()
        );

    private static Action<IInputField> ReportErrorFactory(IMiddlewareContext context, Path contextPath) =>
        argument =>
        {
            if (context.ArgumentValue<object>(argument.Name) is not { } item)
                return;

            var validationResults = new List<ValidationResult>();
            var (success, valueValidation) = ValidateItem(
                argument.ContextData,
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
                contextPath
                    .ToList()
                    .OfType<string>()
                    .ToList();

            foreach (var validationResult in validationResults)
            {
                if (!validationResult.MemberNames.Any())
                {
                    ReportError(
                        context,
                        argument,
                        contextPathList,
                        valueValidation,
                        validationResult.ErrorMessage
                    );
                    continue;
                }

                foreach (var memberName in validationResult.MemberNames)
                {
                    ReportError(
                        context,
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

    private static (bool success, bool? valueValidation) ValidateItem(
        IReadOnlyDictionary<string, object?> context,
        object item,
        string itemName,
        IServiceProvider serviceProvider,
        List<ValidationResult> validationResults
    )
    {
        try
        {
            return context.TryGetValue(nameof(ValidationAttribute), out var attrs) && attrs is IEnumerable<ValidationAttribute> attributes
                ?
                (
                    success: Validator.TryValidateValue(
                        item,
                        new(item, serviceProvider, default) { MemberName = itemName },
                        validationResults,
                        attributes
                    ),
                    valueValidation: true
                )
                :
                (
                    success: Validator.TryValidateObject(
                        item,
                        new(item, serviceProvider, default),
                        validationResults,
                        true
                    ),
                    valueValidation: false
                );
        }
        catch (InvalidCastException ex) when (ex.Source == "System.ComponentModel.Annotations")
        {
            validationResults.Add(
                new(
                    ex switch
                    {
                        { TargetSite.DeclaringType.Name: { Length: > 0 } name } =>
                            $"{ex.Message[0..^1]} for validation attribute {name}",
                        _ => ex.Message
                    }
                )
            );

            return (success: false, valueValidation: default);
        }
    }

    private static void ValidateInputs(IMiddlewareContext context)
    {
        if (context.Selection.Field.Arguments is not { Count: > 0 } arguments)
            return;

        arguments
            .AsParallel()
            .Where(argument =>
                argument is { }
                && !argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute))
            )
            .ForAll(ReportErrorFactory(context, context.Path));
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        ValidateInputs(context);

        if (!context.HasErrors)
            await _next(context).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    [GeneratedRegex("[\\[\\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex BracketsRegex();
}
