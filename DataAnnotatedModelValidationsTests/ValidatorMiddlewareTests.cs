using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CA1822 // Mark members as static
namespace DataAnnotatedModelValidations.Tests
{
    public class ValidatorMiddlewareTests
    {
        public class Sample
        {
            [Required]
            [MinLength(3)]
            [MaxLength(8)]
            [EmailAddress]
            public string? Email { get; set; }
        }

        public class MockService
        {
            public Sample? Get(string? email) => new() { Email = email };
        }

        public class Query
        {
            public Sample? GetSample(Sample? sample) => sample;

            public Sample GetSampleNonNull(Sample sample) => sample;

            public Sample? GetSampleWithService(Sample? sample, [Service] MockService service) =>
                service.Get(sample?.Email);
        }

        [Theory]
        [InlineData("{ sample(sample: null) { email } }", null)]
        [InlineData("{ sample(sample: { email: null }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"\" }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"ab\" }) { email } }", 2)]
        [InlineData("{ sample(sample: { email: \"ab@cd.com\" }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"a@b.com\" }) { email } }", null)]
        [InlineData("{ sampleNonNull(sample: null) { email } }", 1)]
        [InlineData("{ sampleNonNull(sample: { email: \"ab\" }) { email } }", 2)]
        [InlineData("{ sampleWithService(sample: { email: \"abc\" }) { email } }", 1)]
        [InlineData("{ sampleWithService(sample: { email: \"a@b.com\" }) { email } }", null)]
        public async Task InvokeAsyncShouldContainErrors(string query, int? numberOfErrors)
        {
            var result =
               await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQL()
                .UseField<ValidatorMiddleware>()
                .AddQueryType<Query>()
                .ExecuteRequestAsync(query)
                .ConfigureAwait(true);

            Assert.Equal(numberOfErrors, result.Errors?.Count);
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
