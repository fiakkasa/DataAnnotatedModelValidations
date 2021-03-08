using HotChocolate;
using HotChocolate.Resolvers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DataAnnotatedModelValidations
{
    public class ValidatorMiddleware
    {
        private readonly FieldDelegate _next;

        public ValidatorMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        private static void ValidateInputs(IMiddlewareContext context)
        {
            var arguments = context.Field.Arguments;

            if (arguments.Count == 0) return;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            var contextPath = context.Path.ToList().Select(node => new NameString($"{node}")).ToList();

            var errors = new List<ValidationResult>();
            var validationResults =
                arguments
                    .SelectMany(argument =>
                    {
                        errors.Clear();

                        if (argument is not { } || context.ArgumentValue<object>(argument.Name) is not { } obj)
                            return Enumerable.Empty<IError>();

                        Validator.TryValidateObject(obj, new ValidationContext(obj), errors, true);

                        return errors.Select(validationResult =>
                            ErrorBuilder.New()
                                .SetMessage($"{validationResult.ErrorMessage}")
                                .SetPath(
                                    contextPath
                                        .Skip(1)
                                        .Concat(
                                            validationResult.MemberNames.FirstOrDefault() is string propertyName && propertyName.Length > 0
                                                ? new[]
                                                {
                                                        new NameString(argument.Name),
                                                        new NameString($"{char.ToLowerInvariant(propertyName[0])}{textInfo.ToTitleCase(propertyName)[1..]}")
                                                }
                                                : new[] { new NameString(argument.Name) }
                                        )
                                        .Aggregate(Path.New(contextPath[0]), (path, segment) => path.Append(segment))
                                )
                                .SetExtension("field", argument.Coordinate.FieldName)
                                .SetExtension("type", argument.Coordinate.TypeName)
                                .Build()
                        );
                    });

            foreach (var item in validationResults)
                context.ReportError(item);
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            ValidateInputs(context);

            await _next(context).ConfigureAwait(false);
        }
    }
}
