using Microsoft.Extensions.DependencyInjection;

namespace DataAnnotatedModelValidations.Tests;

public class DataAnnotationsValidatorRegistrationTests
{
    [Fact]
    public void AddDataAnnotationsValidator()
    {
        var requestExecutorBuilder = new ServiceCollection().AddGraphQL();
        var count = requestExecutorBuilder.Services.Count;

        requestExecutorBuilder.AddDataAnnotationsValidator();

        // Interceptor + Validator + Previous
        Assert.Equal(count + 2, requestExecutorBuilder.Services.Count);
    }
}
