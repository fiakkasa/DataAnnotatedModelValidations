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

namespace DataAnnotatedModelValidations
{
    public partial class ValidatorMiddleware
    {
        private readonly FieldDelegate _next;

        private static readonly Regex _bracketsRegex = BracketsRegex();

        public ValidatorMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        private static NamePathSegment GenerateArgumentPath(NameString name, string? memberName, List<NameString> contextPath) =>
            contextPath
                .Skip(1)
                .Concat(
                    memberName?.Trim() switch
                    {
                        { Length: > 0 } trimmedMemberName =>
                            trimmedMemberName
                                .Split(':')
                                .Select(x => new NameString(
                                        _bracketsRegex.Replace(x!.Camelize(), "_")
                                    )
                                )
                                .Prepend(name),
                        _ => new[] { name }
                    }
                )
                .Aggregate(Path.New(contextPath[0]), (path, segment) => path.Append(segment));

        private static void ReportError(
            IMiddlewareContext context,
            IInputField argument,
            List<NameString> contextPathList,
            string? message = default,
            string? memberName = default
        ) =>
            context.ReportError(
                ErrorBuilder.New()
                    .SetMessage(message ?? "Unspecified Error")
                    .SetCode("DAMV-400")
                    .SetPath(GenerateArgumentPath(argument.Name, memberName, contextPathList))
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

                if (ValidateItem(argument.ContextData, item, context.Services, validationResults))
                {
                    validationResults.Clear();
                    return;
                }

                var contextPathList =
                    contextPath
                        .ToList()
                        .OfType<NameString>()
                        .ToList();

                foreach (var validationResult in validationResults)
                {
                    if (!validationResult.MemberNames.Any())
                    {
                        ReportError(context, argument, contextPathList, validationResult.ErrorMessage);
                        continue;
                    }

                    foreach (var memberName in validationResult.MemberNames)
                        ReportError(context, argument, contextPathList, validationResult.ErrorMessage, memberName);
                }

                validationResults.Clear();
            };

        private static bool ValidateItem(IReadOnlyDictionary<string, object?> context, object item, IServiceProvider serviceProvider, List<ValidationResult> validationResults)
        {
            try
            {
                return context.TryGetValue(nameof(ValidationAttribute), out var attrs) && attrs is IEnumerable<ValidationAttribute> attributes
                    ? Validator.TryValidateValue(item, new(item, serviceProvider, default), validationResults, attributes)
                    : Validator.TryValidateObject(item, new(item, serviceProvider, default), validationResults, true);
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

                return false;
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
}
