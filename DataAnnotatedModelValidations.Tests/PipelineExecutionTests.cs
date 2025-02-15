using DataAnnotatedModelValidations.Attributes;

namespace DataAnnotatedModelValidations.Tests;

public class PipelineExecutionTests
{
    [Theory]
    [InlineData("""
        {
            samples { 
                items { 
                    psf1: relatedpsf(
                        take: 1
                        skip: 1
                        where: { email: { neq: "t@t.com" } }
                        order: { email: ASC }
                    ) { items { email } }
                    psf2: relatedpsf(
                        skip: 1
                        where: { email: { neq: "t@t.com" } }
                        order: { email: ASC }
                    ) { items { email } }
                    psf3: relatedpsf(
                        where: { email: { neq: "t@t.com" } }
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
        }       
        """,
        "parent_item_with_children"
    )]
    [InlineData("""
        {
            sampleResponses(
                percentage: 10
                take: 5
                order: { email: ASC }
                where: { name: { neq: "some name" } }
            ) {
                items {
                    name
                    email
                    numberOfPets
                    age
                }
            }
        }       
        """,
        "with_query_context"
    )]
    public async Task Get_Data_With_Filter_Sort_And_Or_Pagination_Definitions_Present_Should_Return_Expected_Data(string query, string description)
    {
        var requestExecutor =
            await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQLServer()
                .ModifyPagingOptions(options =>
                {
                    options.RequirePagingBoundaries = false;
                    options.DefaultPageSize = 10;
                    options.MaxPageSize = 100;
                })
                .ModifyCostOptions(options => options.EnforceCostLimits = false)
                .AddDataAnnotationsValidator()
                .AddQueryType<Query>()
                .AddTypeExtension<SampleExtension>()
                .AddSorting()
                .AddFiltering()
                .AddQueryContext()
                .BuildRequestExecutorAsync();

        var result = await requestExecutor.ExecuteAsync(query);

        Assert.Null(result.ExpectOperationResult().Errors);
        result.ExpectOperationResult().ToJson().MatchSnapshot(new SnapshotNameExtension($"{description}.snap"));
    }

    [Theory]
    [InlineData("{ info }", null, "info")]
    [InlineData("{ message }", null, "message")]
    [InlineData("{ invalidRecord(obj: { text: \"test\" }) { text } }", 1, "invalid_record")]
    [InlineData("{ invalidRecordExt(obj: { text: \"test\" }) { text } }", 1, "invalid_record_ext")]
    [InlineData("{ sample(obj: null) { email } }", null, "sample_null_no_errors")]
    [InlineData("{ sample(obj: { email: \"\" }) { email } }", 1, "sample_blank_email_required")]
    [InlineData("{ sample(obj: { email: \"a@b.com\" }) { email } }", null, "sample_no_errors")]
    [InlineData("{ sample(obj: { email: \"ab\" }) { email } }", 2, "sample_min_length_3_and_valid_email")]
    [InlineData("{ sample(obj: { email: \"empty-property-name@b.com\" }) { email } }", 1, "sample_empty-property-name_custom_validation")]
    [InlineData("{ sample(obj: { email: \"message-from-service@b.com\" }) { email } }", 1, "sample_message-from-service_custom_validation")]
    [InlineData("{ sample(obj: { email: \"multiple-property-names@b.com\" }) { email } }", 4, "sample_multiple-property-names_custom_validation")]
    [InlineData("{ sample(obj: { email: \"no-property-name@b.com\" }) { email } }", 1, "sample_no-property-name_custom_validation")]
    [InlineData("{ sample(obj: { email: \"null-error-message@b.com\" }) { email } }", 1, "sample_null-error-message_custom_validation")]
    [InlineData("{ sample(obj: { email: null }) { email } }", 1, "sample_required")]
    [InlineData("{ sampleAlias:sample(obj: { email: \"\" }) { email } }", 1, "sample_alias_blank_email_required")]
    [InlineData("{ sampleIgnoreValidation(obj: null) { email } }", null, "sampleIgnoreValidation_no_errors")]
    [InlineData("{ sampleNonNull(obj: null) { email } }", 1, "sampleNonNull_required")]
    [InlineData("{ sampleNonNull(obj: { email: \"ab\" }) { email } }", 2, "sampleNonNull_min_length_3_and_valid_email")]
    [InlineData("{ sampleWithService(obj: { email: \"a@b.com\" }) { email } }", null, "sampleWithService_no_errors")]
    [InlineData("{ sampleWithService(obj: { email: \"abc\" }) { email } }", 1, "sampleWithService_valid_email")]
    [InlineData("{ text(txt: \"abc\") }", 1, "text_min_length_5")]
    [InlineData("{ text(txt: \"abcdefg\") }", null, "text_no_errors")]
    [InlineData("{ textAlias:text(txt: \"abc\") }", 1, "text_alias_min_length_5")]
    [InlineData("{ textIgnoreValidation(txt: \"a\") }", null, "textIgnoreValidation_no_errors")]
    [InlineData("mutation { setSample(obj: { email: \"\" }) { email } }", 1, "setSample_blank_email_required")]
    [InlineData("mutation { setSampleRecord(obj: { email: \"\" }) { email } }", 1, "setSampleRecord_blank_email_required")]
    [InlineData("mutation { setSampleRecordInline(obj: { email: \"\" }) { email } }", 1, "setSampleRecordInline_blank_email_required")]
    [InlineData("mutation { setSampleRecordWithSynthesizedProperty(obj: { email: \"\" }) { email } }", 1, "setSampleRecordWithSynthesizedProperty_blank_email_required")]
    [InlineData("mutation { setText(txt: \"abc\") }", 1, "setText_min_length_5")]
    [InlineData("""
        mutation { 
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
        }
        """,
        3,
        "setNestedParent_nested_validations"
    )]
    [InlineData("""
        mutation { 
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
        }
        """,
        3,
        "setNestedParentExt_nested_validations_ext"
    )]
    public async Task Validation_Should_Return_Expected_Errors(string query, int? numberOfErrors, string description)
    {
        var result =
            await new ServiceCollection()
                .AddSingleton<MockService>()
                .AddGraphQLServer()
                .AddDataAnnotationsValidator()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddTypeExtension<QueryExtension>()
                .AddTypeExtension<MutationExtension>()
                .AddSorting()
                .AddFiltering()
                .ExecuteRequestAsync(query)
                .ConfigureAwait(true);

        Assert.Equal(numberOfErrors, result.ExpectOperationResult().Errors?.Count);
        result.ExpectOperationResult().ToJson().MatchSnapshot(new SnapshotNameExtension($"{description}.snap"));
    }

    public record SampleResponse
    {
        public string Name { get; init; } = string.Empty;
        public string? Email { get; init; }
        public int NumberOfPets { get; init; }
        public int? Age { get; init; }
    }

    public record SampleRecordWithSynthesizedProperty(
        [property: Required]
        [property: MinLength(3)]
        [property: EmailAddress]
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
            {
                yield return new("no-property-name");
            }

            if (Email?.StartsWith("empty-property-name") == true)
            {
                yield return new("empty-property-name", [string.Empty]);
            }

            if (Email?.StartsWith("null-error-message") == true)
            {
                yield return new(null);
            }

            if (Email?.StartsWith("multiple-property-names") == true)
            {
                yield return new("multiple-property-names",
                [
                    "hello",
                    string.Empty,
                    null!,
                    "world"
                ]);
            }

            if (Email?.StartsWith("message-from-service") == true)
            {
                yield return new(validationContext.GetRequiredService<MockService>().Message);
            }
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

        public List<NestedChild> Children { get; set; } = [];

        [GraphQLIgnore]
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResultsOfChild = new List<ValidationResult>();

            Validator.TryValidateObject(Child, new(Child, null), validationResultsOfChild, true);

            foreach (var item in validationResultsOfChild)
            {
                yield return new(item.ErrorMessage, item.MemberNames.Prepend(nameof(Child)));
            }

            var validationResultOfChildren = new List<ValidationResult>();

            var index = 0;
            foreach (var item in Children)
            {
                validationResultsOfChild.Clear();
                Validator.TryValidateObject(item, new(item, null), validationResultsOfChild, true);
                validationResultOfChildren.AddRange(
                    validationResultsOfChild.Select(childValidationResult =>
                        new ValidationResult(
                            childValidationResult.ErrorMessage,
                            childValidationResult
                                .MemberNames
                                .Select(memberName => $"{nameof(Children)}:[{index}]:{memberName}")
                                .ToArray()
                        )
                    )
                );
                index++;
            }

            foreach (var item in validationResultOfChildren)
            {
                yield return item;
            }
        }
    }

    public record InvalidRecord
    {
        [MaxLength(10)]
        public Optional<string> Text { get; init; }
    }

    [ExtendObjectType<Sample>]
    public class SampleExtension
    {
        [UseOffsetPaging]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Relatedpsf => new Sample[]
        {
            new()
            {
                Email = "a@a.com"
            },
            new()
            {
                Email = "b@a.com"
            }
        }.AsQueryable();

        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Relatedsf => new Sample[]
        {
            new()
            {
                Email = "a@a.com"
            },
            new()
            {
                Email = "b@a.com"
            }
        }.AsQueryable();

        [UseOffsetPaging]
        [UseFiltering]
        public IQueryable<Sample> Relatedp => new Sample[]
        {
            new()
            {
                Email = "a@a.com"
            },
            new()
            {
                Email = "b@a.com"
            }
        }.AsQueryable();
    }

    public class MockService
    {
        public string Message { get; } = "Splash!";

        public Sample? Get(string? email) => new()
        {
            Email = email
        };
    }

    public class Query
    {
        public string Info => "Info";

        [UseOffsetPaging]
        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Samples => new Sample[]
        {
            new()
            {
                Email = "a@a.com"
            }
        }.AsQueryable();

        public string GetMessage() => "Message";

        [UseOffsetPaging]
        [UseSorting]
        [UseFiltering]
        public async Task<List<SampleResponse>> GetSampleResponses(
            [Range(0, 100)] double percentage,
            QueryContext<SampleResponse> queryContext,
            CancellationToken cancellationToken
        )
        {
            await Task.CompletedTask;

            return Enumerable
                .Range(0, 4)
                .Select(i =>
                    i switch
                    {
                        0 => new SampleResponse
                        {
                            Name = "John Doe",
                            Email = "john@doe.com",
                            NumberOfPets = 2,
                            Age = 30
                        },
                        1 => new SampleResponse
                        {
                            Name = "Jane Doe",
                            NumberOfPets = 1,
                            Age = 25
                        },
                        2 => new SampleResponse
                        {
                            Name = "Bob Smith",
                            Email = "bob@smith.com",
                            NumberOfPets = 0
                        },
                        3 => new SampleResponse
                        {
                            Name = "Alice Johnson"
                        },
                        _ => new SampleResponse
                        {
                            Email = "user@email.com"
                        }
                    }
                )
                .AsQueryable()
                .With(queryContext)
                .ToList();
        }

        public string? GetText([MinLength(5)] string? txt) => txt;

        public string? GetTextIgnoreValidation([IgnoreModelValidation] [MinLength(5)] string? txt) => txt;

        public Sample? GetSample(Sample? obj) => obj;

        public Sample GetSampleNonNull(Sample obj) => obj;

        public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? obj) => obj;

        public Sample? GetSampleWithService(Sample? obj, [Service] MockService service) =>
            service.Get(obj?.Email);

        public InvalidRecord GetInvalidRecord(InvalidRecord obj) => obj;
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtension
    {
        public InvalidRecord GetInvalidRecordExt([Parent] Query parent, InvalidRecord obj) => parent.GetInvalidRecord(obj);
    }

    public class Mutation
    {
        public string? SetText([MinLength(5)] string? txt) => txt;

        public Sample? SetSample(Sample? obj) => obj;

        public NestedParent SetNestedParent(NestedParent obj) => obj;

        public SampleRecord? SetSampleRecord(SampleRecord? obj) => obj;

        public SampleRecordWithSynthesizedProperty? SetSampleRecordWithSynthesizedProperty(SampleRecordWithSynthesizedProperty? obj) => obj;
    }

    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class MutationExtension
    {
        public NestedParent SetNestedParentExt([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
    }
}
