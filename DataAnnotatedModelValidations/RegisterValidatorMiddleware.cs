using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataAnnotatedModelValidations;

public static class RegisterValidatorMiddleware
{
    public static IRequestExecutorBuilder AddDataAnnotationsValidator(this IRequestExecutorBuilder requestExecutorBuilder) =>
        requestExecutorBuilder
            .TryAddTypeInterceptor<ValidatorTypeInterceptor>()
            .UseField<ValidatorMiddleware>();
}
