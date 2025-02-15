namespace DataAnnotatedModelValidations.Tests;

public class DataAnnotationsValidatorRegistrationTests
{
    [Fact]
    public void AddDataAnnotationsValidator_Should_Add_Interceptor_To_GraphQLBuilder()
    {
        var requestExecutorBuilder = new ServiceCollection().AddGraphQL();
        var count = requestExecutorBuilder.Services.Count;

        requestExecutorBuilder.AddDataAnnotationsValidator();

        // Interceptor + Previous
        Assert.Equal(count + 1, requestExecutorBuilder.Services.Count);
    }
}
