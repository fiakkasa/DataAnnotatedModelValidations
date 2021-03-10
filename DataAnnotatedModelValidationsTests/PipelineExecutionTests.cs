using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CA1822 // Mark members as static
namespace DataAnnotatedModelValidations.Tests
{
    public class PipelineExecutionTests
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
            public string? GetText([MinLength(5)] string? txt) => txt;

            public string? GetTextIgnoreValidation([IgnoreModelValidation][MinLength(5)] string? txt) => txt;

            public Sample? GetSample(Sample? obj) => obj;

            public Sample GetSampleNonNull(Sample obj) => obj;

            public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? obj) => obj;

            public Sample? GetSampleWithService(Sample? obj, [Service] MockService service) =>
                service.Get(obj?.Email);
        }

        [Theory]
        [InlineData("{ text(txt: \"abc\") }", 1, "text_min_length_5")]
        [InlineData("{ textAlias:text(txt: \"abc\") }", 1, "text_alias_min_length_5")]
        [InlineData("{ text(txt: \"abcdefg\") }", null, "text_no_errors")]
        [InlineData("{ textIgnoreValidation(txt: \"a\") }", null, "textIgnoreValidation_no_errors")]
        [InlineData("{ sample(obj: null) { email } }", null, "sample_null_no_errors")]
        [InlineData("{ sample(obj: { email: null }) { email } }", 1, "sample_required")]
        [InlineData("{ sample(obj: { email: \"\" }) { email } }", 1, "sample_blank_email_required")]
        [InlineData("{ sampleAlias:sample(obj: { email: \"\" }) { email } }", 1, "sample_alias_blank_email_required")]
        [InlineData("{ sample(obj: { email: \"ab\" }) { email } }", 2, "sample_min_length_3_and_valid_email")]
        [InlineData("{ sample(obj: { email: \"no-property-name@b.com\" }) { email } }", 1, "sample_no-property-name_custom_validation")]
        [InlineData("{ sample(obj: { email: \"empty-property-name@b.com\" }) { email } }", 1, "sample_empty-property-name_custom_validation")]
        [InlineData("{ sample(obj: { email: \"a@b.com\" }) { email } }", null, "sample_no_errors")]
        [InlineData("{ sampleNonNull(obj: null) { email } }", 1, "sampleNonNull_required")]
        [InlineData("{ sampleNonNull(obj: { email: \"ab\" }) { email } }", 2, "sampleNonNull_min_length_3_and_valid_email")]
        [InlineData("{ sampleIgnoreValidation(obj: null) { email } }", null, "sampleIgnoreValidation_no_errors")]
        [InlineData("{ sampleWithService(obj: { email: \"abc\" }) { email } }", 1, "sampleWithService_valid_email")]
        [InlineData("{ sampleWithService(obj: { email: \"a@b.com\" }) { email } }", null, "sampleWithService_no_errors")]
        public async Task Validation(string query, int? numberOfErrors, string description)
        {
            var result =
                await new ServiceCollection()
                    .AddSingleton<MockService>()
                    .AddGraphQL()
                    .TryAddTypeInterceptor<ValidatorTypeInterceptor>()
                    .UseField<ValidatorMiddleware>()
                    .AddQueryType<Query>()
                    .ExecuteRequestAsync(query)
                    .ConfigureAwait(true);

            Assert.Equal(numberOfErrors, result.Errors?.Count);
            (await result.ToJsonAsync().ConfigureAwait(false)).MatchSnapshot(new SnapshotNameExtension($"{description}.snap"));
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
