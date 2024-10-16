using DataAnnotatedModelValidations.Extensions;

namespace DataAnnotatedModelValidations.Middleware;

public sealed class ValidatorMiddleware(FieldDelegate next)
{
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        context.ValidateInputs();

        if (!context.HasErrors)
        {
            await next(context);
        }
    }
}
