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
    public class ValidatorMiddleware
    {
        private readonly FieldDelegate _next;

        private static readonly Regex _bracketsRegex = new(@"[\[\]]+", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex _lastUnderscoreRegex = new("_$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public ValidatorMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        private static NamePathSegment GenerateArgumentPath(NameString name, List<NameString> contextPath, ValidationResult validationResult) =>
            contextPath
                .Skip(1)
                .Concat(
                    (validationResult.MemberNames.FirstOrDefault() is { Length: > 0 }) switch
                    {
                        true =>
                            validationResult
                                .MemberNames
                                .Select(x => x?.Trim())
                                .Where(x => x is { Length: > 0 })
                                .Select(x => new NameString(
                                        _lastUnderscoreRegex.Replace(
                                            _bracketsRegex.Replace(x!.Camelize(), "_"),
                                            string.Empty
                                        )
                                    )
                                )
                                .Prepend(name),
                        _ => new[] { name }
                    }
                )
                .Aggregate(Path.New(contextPath[0]), (path, segment) => path.Append(segment));

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
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage(validationResult.ErrorMessage ?? "Unspecified Error")
                            .SetCode("DAMV-400")
                            .SetPath(GenerateArgumentPath(argument.Name, contextPathList, validationResult))
                            .SetExtension("field", argument.Coordinate.FieldName)
                            .SetExtension("type", argument.Coordinate.TypeName)
                            .SetExtension("specifiedBy", "http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type")
                            .Build()
                    );
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
    }
}
