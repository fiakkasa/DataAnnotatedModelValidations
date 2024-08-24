using DataAnnotatedModelValidations.Middleware;
using DataAnnotatedModelValidations.TypeInterceptors;

namespace DataAnnotatedModelValidations;

public static class DataAnnotationsValidatorRegistration
{
    public static IRequestExecutorBuilder AddDataAnnotationsValidator(this IRequestExecutorBuilder requestExecutorBuilder) =>
        requestExecutorBuilder
            .TryAddTypeInterceptor<ValidatorTypeInterceptor>()
            .UseField<ValidatorMiddleware>();
}
