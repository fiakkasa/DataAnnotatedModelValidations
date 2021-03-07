using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DataAnnotatedModelValidations.Tests
{
    public class RegisterValidatorMiddlewareTests
    {
        [Fact]
        public void AddDataAnnotationsValidator()
        {
            var serviceCollection = new ServiceCollection();
            var mockRequestExecutorBuilder = new Mock<IRequestExecutorBuilder>();
            mockRequestExecutorBuilder.SetupGet(p => p.Services).Returns(serviceCollection);

            mockRequestExecutorBuilder.Object.AddDataAnnotationsValidator();

            Assert.True(serviceCollection.Count > 0);
        }
    }
}