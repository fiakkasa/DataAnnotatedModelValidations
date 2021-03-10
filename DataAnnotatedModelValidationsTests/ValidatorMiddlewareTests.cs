using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CA1822 // Mark members as static
namespace DataAnnotatedModelValidations.Tests
{
    public class ValidatorMiddlewareTests
    {
        public class Sample : IValidatableObject
        {
            [Required]
            [MinLength(3)]
            [EmailAddress]
            public string? Email { get; set; }

            [GraphQLIgnore]
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
                (
                    Email?.StartsWith("no-property-name") == true
                        ? new[] { new ValidationResult("no-property-name") }
                        : Enumerable.Empty<ValidationResult>()
                )
                .Concat(
                    Email?.StartsWith("empty-property-name") == true
                        ? new[] { new ValidationResult("empty-property-name", new[] { "" }) }
                        : Enumerable.Empty<ValidationResult>()
                );
        }

        public class MockService
        {
            public Sample? Get(string? email) => new() { Email = email };
        }

        public class Query
        {
            public string? GetText([MinLength(5)] string? text) => text;

            public string? GetTextIgnoreValidation([IgnoreModelValidation][MinLength(5)] string? text) => text;

            public Sample? GetSample(Sample? sample) => sample;

            public Sample GetSampleNonNull(Sample sample) => sample;

            public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? sample) => sample;

            public Sample? GetSampleWithService(Sample? sample, [Service] MockService service) =>
                service.Get(sample?.Email);
        }

        [Theory]
        [InlineData("{ text(text: null) }", 1)]
        [InlineData("{ text(text: \"abcdefg\") }", null)]
        [InlineData("{ textIgnoreValidation(text: \"a\") }", null)]
        [InlineData("{ sample(sample: null) { email } }", null)]
        [InlineData("{ sample(sample: { email: null }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"\" }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"ab\" }) { email } }", 2)]
        [InlineData("{ sample(sample: { email: \"no-property-name@b.com\" }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"empty-property-name@b.com\" }) { email } }", 1)]
        [InlineData("{ sample(sample: { email: \"a@b.com\" }) { email } }", null)]
        [InlineData("{ sampleNonNull(sample: null) { email } }", 1)]
        [InlineData("{ sampleNonNull(sample: { email: \"ab\" }) { email } }", 2)]
        [InlineData("{ sampleIgnoreValidation(sample: null) { email } }", null)]
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
