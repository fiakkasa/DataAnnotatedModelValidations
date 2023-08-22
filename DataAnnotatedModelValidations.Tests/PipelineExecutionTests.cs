using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Mark members as static
namespace DataAnnotatedModelValidations.Tests;

public class PipelineExecutionTests
{
    public record SampleRecordInline(
        [property:Required]
        [property:MinLength(3)]
        [property:EmailAddress]
        string? Email
    );

    public record SampleRecord
    {
        [Required]
        [MinLength(3)]
        [EmailAddress]
        public string? Email { get; set; }
    }

    public class Sample : IValidatableObject
    {
        [Required]
        [MinLength(3)]
        [EmailAddress]
        public string? Email { get; set; }

        [GraphQLIgnore]
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Email?.StartsWith("no-property-name") == true)
                yield return new("no-property-name");

            if (Email?.StartsWith("empty-property-name") == true)
                yield return new("empty-property-name", new[] { string.Empty });

            if (Email?.StartsWith("null-error-message") == true)
                yield return new(null);

            if (Email?.StartsWith("multiple-property-names") == true)
                yield return new("multiple-property-names", new[] { "hello", string.Empty, null!, "world" });

            if (Email?.StartsWith("message-from-service") == true)
                yield return new(validationContext.GetRequiredService<MockService>().Message);
        }
    }

    public record NestedChild
    {
        [Range(1, 10)]
        public int Count { get; set; }
    }

    public record NestedParent : IValidatableObject
    {
        public NestedChild Child { get; set; } = new();

        public List<NestedChild> Children { get; set; } = new();

        [GraphQLIgnore]
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResultsOfChild = new List<ValidationResult>();

            Validator.TryValidateObject(Child, new(Child, null), validationResultsOfChild, true);

            foreach (var item in validationResultsOfChild)
                yield return new(item.ErrorMessage, item.MemberNames.Prepend(nameof(Child)));

            var validationResultOfChildren = new List<ValidationResult>();

            var index = 0;
            foreach (var item in Children)
            {
                validationResultsOfChild.Clear();
                Validator.TryValidateObject(item, new(item, null), validationResultsOfChild, true);
                validationResultOfChildren.AddRange(
                    validationResultsOfChild.Select(x => new ValidationResult(x.ErrorMessage, x.MemberNames.Select(x => $"{nameof(Children)}:[{index}]:{x}")))
                 );
                index++;
            }

            foreach (var item in validationResultOfChildren)
                yield return item;
        }
    }

    public record InvalidRecord
    {
        [MaxLength(10)]
        public Optional<string> Text { get; init; }
    }

    [ExtendObjectType(typeof(Sample))]
    public class SampleExtension
    {
        [UseOffsetPaging]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Relatedpsf => new Sample[] { new() { Email = "a@a.com" }, new() { Email = "b@a.com" } }.AsQueryable();

        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Relatedsf => new Sample[] { new() { Email = "a@a.com" }, new() { Email = "b@a.com" } }.AsQueryable();

        [UseOffsetPaging]
        [UseFiltering]
        public IQueryable<Sample> Relatedp => new Sample[] { new() { Email = "a@a.com" }, new() { Email = "b@a.com" } }.AsQueryable();
    }

    public class MockService
    {
        public Sample? Get(string? email) => new() { Email = email };

        public string Message { get; } = "Splash!";
    }

    public class Query
    {
        public string Info => "Info";

        public string? GetText([MinLength(5)] string? txt) => txt;

        public string? GetTextIgnoreValidation([IgnoreModelValidation][MinLength(5)] string? txt) => txt;

        public Sample? GetSample(Sample? obj) => obj;

        public Sample GetSampleNonNull(Sample obj) => obj;

        public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? obj) => obj;

        public Sample? GetSampleWithService(Sample? obj, [Service] MockService service) =>
            service.Get(obj?.Email);

        [UseOffsetPaging]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Samples => new Sample[] { new() }.AsQueryable();

        public InvalidRecord GetInvalidRecord(InvalidRecord obj) => obj;
    }

    [ExtendObjectType(nameof(Query))]
    public class ExtendedQuery : Query
    {
        public InvalidRecord GetInvalidRecordExt([Parent] Query parent, InvalidRecord obj) => parent.GetInvalidRecord(obj);
    }

    public class Mutation
    {
        public string? SetText([MinLength(5)] string? txt) => txt;

        public Sample? SetSample(Sample? obj) => obj;

        public NestedParent SetNestedParent(NestedParent obj) => obj;

        public SampleRecord? SetSampleRecord(SampleRecord? obj) => obj;

        public SampleRecordInline? SetSampleRecordInline(SampleRecordInline? obj) => obj;
    }

    [ExtendObjectType(nameof(Mutation))]
    public class ExtendedMutation : Mutation
    {
        public NestedParent SetNestedParentExt([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
    }

    [Fact(DisplayName = "No Error when Filter, Sort, And - Or Pagination Definitions Present")]
    public async Task NoErrorFilterSortAndOrPaginationDefinitionsPresent()
    {
        const string query =
@"{ 
    samples { 
        items { 
            psf1: relatedpsf(
                take: 1
                skip: 1
                where: { email: { neq: ""t@t.com"" } }
                order: { email: ASC }
            ) { items { email } }
            psf2: relatedpsf(
                skip: 1
                where: { email: { neq: ""t@t.com"" } }
                order: { email: ASC }
            ) { items { email } }
            psf3: relatedpsf(
                where: { email: { neq: ""t@t.com"" } }
                order: { email: ASC }
            ) { items { email } }
            psf4: relatedpsf(
                order: { email: ASC }
            ) { items { email } }
            psf5: relatedpsf { items { email } }
            relatedsf { email }
            relatedp { items { email } }
        } 
    } 
}";
        var result =
            await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQLServer()
                .AddDataAnnotationsValidator()
                .AddQueryType<Query>()
                .AddTypeExtension<SampleExtension>()
                .AddSorting()
                .AddFiltering()
                .ExecuteRequestAsync(query)
                .ConfigureAwait(true);

        Assert.Null(result.ExpectQueryResult().Errors);
        result.ExpectQueryResult().ToJson().MatchSnapshot();
    }

    [Theory]
    [InlineData("{ invalidRecord(obj: { text: \"test\" }) { text } }", 1, "invalid_record")]
    [InlineData("{ info }", null, "info")]
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
    [InlineData("{ sample(obj: { email: \"multiple-property-names@b.com\" }) { email } }", 4, "sample_multiple-property-names_custom_validation")]
    [InlineData("{ sample(obj: { email: \"null-error-message@b.com\" }) { email } }", 1, "sample_null-error-message_custom_validation")]
    [InlineData("{ sample(obj: { email: \"message-from-service@b.com\" }) { email } }", 1, "sample_message-from-service_custom_validation")]
    [InlineData("{ sample(obj: { email: \"a@b.com\" }) { email } }", null, "sample_no_errors")]
    [InlineData("{ sampleNonNull(obj: null) { email } }", 1, "sampleNonNull_required")]
    [InlineData("{ sampleNonNull(obj: { email: \"ab\" }) { email } }", 2, "sampleNonNull_min_length_3_and_valid_email")]
    [InlineData("{ sampleIgnoreValidation(obj: null) { email } }", null, "sampleIgnoreValidation_no_errors")]
    [InlineData("{ sampleWithService(obj: { email: \"abc\" }) { email } }", 1, "sampleWithService_valid_email")]
    [InlineData("{ sampleWithService(obj: { email: \"a@b.com\" }) { email } }", null, "sampleWithService_no_errors")]
    [InlineData("mutation { setSample(obj: { email: \"\" }) { email } }", 1, "setSample_blank_email_required")]
    [InlineData("mutation { setSampleRecord(obj: { email: \"\" }) { email } }", 1, "setSampleRecord_blank_email_required")]
    [InlineData("mutation { setSampleRecordInline(obj: { email: \"\" }) { email } }", 1, "setSampleRecordInline_blank_email_required")]
    [InlineData("mutation { setText(txt: \"abc\") }", 1, "setText_min_length_5")]
    [InlineData(@"mutation { 
            setNestedParent(obj: { 
                child: { count: 0 }, 
                children: [
                    { count: 1 }, 
                    { count: 0 }
                ] 
            }) { 
                child { count } 
                children { count } 
            } 
        }", 3, "setNestedParent_nested_validations")]
    public async Task ValidationClassBased(string query, int? numberOfErrors, string description)
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQLServer()
                .AddDataAnnotationsValidator()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSorting()
                .AddFiltering()
                .ExecuteRequestAsync(query)
                .ConfigureAwait(true);

        Assert.Equal(numberOfErrors, result.ExpectQueryResult().Errors?.Count);
        result.ExpectQueryResult().ToJson().MatchSnapshot(new SnapshotNameExtension($"{description}.snap"));
    }

    [Theory]
    [InlineData("{ invalidRecord(obj: { text: \"test\" }) { text } }", 1, "invalid_record")]
    [InlineData("{ invalidRecordExt(obj: { text: \"test\" }) { text } }", 1, "invalid_record_ext")]
    [InlineData("{ info }", null, "info")]
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
    [InlineData("{ sample(obj: { email: \"multiple-property-names@b.com\" }) { email } }", 4, "sample_multiple-property-names_custom_validation")]
    [InlineData("{ sample(obj: { email: \"null-error-message@b.com\" }) { email } }", 1, "sample_null-error-message_custom_validation")]
    [InlineData("{ sample(obj: { email: \"message-from-service@b.com\" }) { email } }", 1, "sample_message-from-service_custom_validation")]
    [InlineData("{ sample(obj: { email: \"a@b.com\" }) { email } }", null, "sample_no_errors")]
    [InlineData("{ sampleNonNull(obj: null) { email } }", 1, "sampleNonNull_required")]
    [InlineData("{ sampleNonNull(obj: { email: \"ab\" }) { email } }", 2, "sampleNonNull_min_length_3_and_valid_email")]
    [InlineData("{ sampleIgnoreValidation(obj: null) { email } }", null, "sampleIgnoreValidation_no_errors")]
    [InlineData("{ sampleWithService(obj: { email: \"abc\" }) { email } }", 1, "sampleWithService_valid_email")]
    [InlineData("{ sampleWithService(obj: { email: \"a@b.com\" }) { email } }", null, "sampleWithService_no_errors")]
    [InlineData("mutation { setSample(obj: { email: \"\" }) { email } }", 1, "setSample_blank_email_required")]
    [InlineData("mutation { setSampleRecord(obj: { email: \"\" }) { email } }", 1, "setSampleRecord_blank_email_required")]
    [InlineData("mutation { setSampleRecordInline(obj: { email: \"\" }) { email } }", 1, "setSampleRecordInline_blank_email_required")]
    [InlineData("mutation { setText(txt: \"abc\") }", 1, "setText_min_length_5")]
    [InlineData(@"mutation { 
            setNestedParent(obj: { 
                child: { count: 0 }, 
                children: [
                    { count: 1 }, 
                    { count: 0 }
                ] 
            }) { 
                child { count } 
                children { count } 
            } 
        }", 3, "setNestedParent_nested_validations")]
    [InlineData(@"mutation { 
            setNestedParentExt(obj: { 
                child: { count: 0 }, 
                children: [
                    { count: 1 }, 
                    { count: 0 }
                ] 
            }) { 
                child { count } 
                children { count } 
            } 
        }", 3, "setNestedParentExt_nested_validations")]
    public async Task ValidationExtendedBased(string query, int? numberOfErrors, string description)
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQLServer()
                .AddDataAnnotationsValidator()
                .AddQueryType(x => x.Name(nameof(Query)))
                .AddMutationType(x => x.Name(nameof(Mutation)))
                .AddTypeExtension<ExtendedQuery>()
                .AddTypeExtension<ExtendedMutation>()
                .AddSorting()
                .AddFiltering()
                .ExecuteRequestAsync(query)
                .ConfigureAwait(true);

        Assert.Equal(numberOfErrors, result.ExpectQueryResult().Errors?.Count);
        result.ExpectQueryResult().ToJson().MatchSnapshot(new SnapshotNameExtension($"{description}.snap"));
    }
}
#pragma warning restore CA1822 // Mark members as static
