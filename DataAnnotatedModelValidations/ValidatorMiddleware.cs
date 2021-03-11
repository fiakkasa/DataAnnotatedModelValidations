using HotChocolate;
using HotChocolate.Resolvers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IInputField = HotChocolate.Types.IInputField;

namespace DataAnnotatedModelValidations
{
    public class ValidatorMiddleware
    {
        private readonly FieldDelegate _next;

        public ValidatorMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        private static NamePathSegment GenerateArgumentPath(NameString name, TextInfo textInfo, List<NameString> contextPath, ValidationResult validationResult) =>
            contextPath
                .Skip(1)
                .Concat(
                    validationResult.MemberNames.FirstOrDefault() is string propertyName
                    && propertyName.Length > 0
                        ? new[]
                        {
                            name,
                            new NameString($"{char.ToLowerInvariant(propertyName[0])}{textInfo.ToTitleCase(propertyName)[1..]}")
                        }
                        : new[] { name }
                )
                .Aggregate(Path.New(contextPath[0]), (path, segment) => path.Append(segment));

        private static Action<IInputField> ReportErrorFactory(IMiddlewareContext context, TextInfo textInfo, List<NameString> contextPath) =>
            argument =>
            {
                if (context.ArgumentValue<object>(argument.Name) is not { } item)
                    return;

                var validationResults = new List<ValidationResult>();

                _ = ValidateItem(argument.ContextData, item, validationResults);

                foreach (var validationResult in validationResults)
                {
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage(validationResult.ErrorMessage ?? "Unspecified Error")
                            .SetCode("DAMV-400")
                            .SetPath(GenerateArgumentPath(argument.Name, textInfo, contextPath, validationResult))
                            .SetExtension("field", argument.Coordinate.FieldName)
                            .SetExtension("type", argument.Coordinate.TypeName)
                            .SetExtension("specifiedBy", "http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type")
                            .Build()
                    );
                }

                validationResults.Clear();
                validationResults = default;
            };

        private static bool ValidateItem(IReadOnlyDictionary<string, object?> context, object item, List<ValidationResult> validationResults) =>
            context.TryGetValue(nameof(ValidationAttribute), out var attrs) && attrs is IEnumerable<ValidationAttribute> attributes
                ? Validator.TryValidateValue(item, new ValidationContext(item), validationResults, attributes)
                : Validator.TryValidateObject(item, new ValidationContext(item), validationResults, true);

        private static void ValidateInputs(IMiddlewareContext context)
        {
            if (context.Field.Arguments is not { Count: > 0 } arguments)
                return;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var contextPath =
                context.Path
                    .ToList()
                    .Select(node => new NameString($"{node}"))
                    .ToList();

            arguments
                .AsParallel()
                .Where(argument =>
                    argument is { }
                    && !argument.ContextData.ContainsKey(nameof(IgnoreModelValidationAttribute))
                )
                .ForAll(ReportErrorFactory(context, textInfo, contextPath));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            ValidateInputs(context);

            if (!context.HasErrors)
                await _next(context).ConfigureAwait(false);
        }
    }
}
