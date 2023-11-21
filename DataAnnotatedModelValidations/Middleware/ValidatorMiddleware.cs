using DataAnnotatedModelValidations.Extensions;
using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace DataAnnotatedModelValidations.Middleware;

public sealed class ValidatorMiddleware
{
    private readonly FieldDelegate _next;

    public ValidatorMiddleware(FieldDelegate next) => _next = next;

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        context.ValidateInputs();

        if (!context.HasErrors)
            await _next(context).ConfigureAwait(false);
    }
}
