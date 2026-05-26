using DataAnnotatedModelValidations.TypeInterceptors;

namespace DataAnnotatedModelValidations;

public static class DataAnnotationsValidatorRegistration
{
    public static IRequestExecutorBuilder AddDataAnnotationsValidator(
        this IRequestExecutorBuilder requestExecutorBuilder,
        bool bindUsingRootTypeFields = true
    ) =>
        requestExecutorBuilder.TryAddTypeInterceptor(
            new ValidatorTypeInterceptor(bindUsingRootTypeFields)
        );
}
