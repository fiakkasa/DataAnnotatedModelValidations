using DataAnnotatedModelValidations.TypeInterceptors;

namespace DataAnnotatedModelValidations;

public static class DataAnnotationsValidatorRegistration
{
    public static IRequestExecutorBuilder AddDataAnnotationsValidator(
        this IRequestExecutorBuilder requestExecutorBuilder,
        bool restrictToRootTypes = true
    ) =>
        requestExecutorBuilder.TryAddTypeInterceptor(
            new ValidatorTypeInterceptor(restrictToRootTypes)
        );
}
