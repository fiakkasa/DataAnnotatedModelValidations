using HotChocolate;
using HotChocolate.Resolvers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
                if (
                    argument is not { }
                    || context.ArgumentValue<object>(argument.Name) is not { } obj
                    || obj.GetType().GetCustomAttribute(typeof(IgnoreModelValidationAttribute)) is { }
                )
                {
                    return;
                }

                var validationResults = new List<ValidationResult>();

                Validator.TryValidateObject(obj, new ValidationContext(obj), validationResults, true);

                foreach (var validationResult in validationResults)
                {
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage($"{validationResult.ErrorMessage}")
                            .SetCode("400")
                            .SetPath(GenerateArgumentPath(argument.Name, textInfo, contextPath, validationResult))
                            .SetExtension("field", argument.Coordinate.FieldName)
                            .SetExtension("type", argument.Coordinate.TypeName)
                            .Build()
                    );
                }

                validationResults.Clear();
                validationResults = default;
            };

        private static void ValidateInputs(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;

            if (arguments.Count == 0) return;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var contextPath =
                context.Path
                    .ToList()
                    .Select(node => new NameString($"{node}"))
                    .ToList();

            arguments
                .AsParallel()
                .ForAll(ReportErrorFactory(context, textInfo, contextPath));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            ValidateInputs(context);

            await _next(context).ConfigureAwait(false);
        }
    }
}
