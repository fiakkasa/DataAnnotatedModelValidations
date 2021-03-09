using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataAnnotatedModelValidations.Tests
{
    public class RegisterValidatorMiddlewareTests
    {
        [Fact]
        public void AddDataAnnotationsValidator()
        {
            var requestExecutorBuilder = new ServiceCollection().AddGraphQL();
            var count = requestExecutorBuilder.Services.Count;

            requestExecutorBuilder.AddDataAnnotationsValidator();

            Assert.Equal(count + 1, requestExecutorBuilder.Services.Count);
        }
    }
}