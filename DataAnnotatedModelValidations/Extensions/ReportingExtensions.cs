using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DataAnnotatedModelValidations.Utils;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Humanizer;

namespace DataAnnotatedModelValidations.Extensions;

internal static class ReportingExtensions
{
    private static readonly Regex _bracketsRegex = RegexUtils.GetBracketsRegex();

    internal static string GetNormalizedMemberName(this string memberName) =>
        _bracketsRegex.Replace(memberName.Camelize(), "_");

    internal static IEnumerable<string> ToTokenizedMemberNames(this string trimmedMemberName) =>
        trimmedMemberName
            .Split(':')
            .Select(GetNormalizedMemberName);

    internal static IEnumerable<string> ToComposedMemberNames(
        this IInputField argument,
        string? memberName,
        bool? valueValidation
    ) =>
        (memberName?.Trim(), valueValidation) switch
        {
            ({ Length: > 0 } trimmedMemberName, true) =>
                trimmedMemberName.ToTokenizedMemberNames(),
            ({ Length: > 0 } trimmedMemberName, _) =>
                trimmedMemberName.ToTokenizedMemberNames().Prepend(argument.Name),
            _ => argument.Name.AsEnumerable()
        };

    internal static Path ToArgumentPath(this List<string> contextPath, IEnumerable<string> composedMemberNames) =>
        contextPath
            .Concat(composedMemberNames)
            .Aggregate(Path.Root, (acc, item) => acc.Append(item));

    internal static void ReportError(
        this IMiddlewareContext context,
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
                .SetPath(
                    contextPathList.ToArgumentPath(
                        argument.ToComposedMemberNames(memberName, valueValidation)
                    )
                )
                .SetExtension("field", argument.Coordinate.FieldName)
                .SetExtension("type", argument.Coordinate.TypeName)
                .SetExtension("specifiedBy", "http://spec.graphql.org/June2018/#sec-Values-of-Correct-Type")
                .Build()
        );
}
