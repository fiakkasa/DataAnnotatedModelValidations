namespace DataAnnotatedModelValidations.Tests.Pipeline;

public partial class PipelineExecutionTests
{
    [Theory]
    [InlineData(
        """
        {
            samples { 
                items { 
                    psf1: relatedpsf(
                        take: 1
                        skip: 1
                        where: { name: { neq: "Alisson" } }
                        order: { name: ASC }
                    ) { items { name } }
                    psf2: relatedpsf(
                        skip: 1
                        where: { name: { neq: "Jane" } }
                        order: { name: ASC }
                    ) { items { name } }
                    psf3: relatedpsf(
                        where: { name: { neq: "Jane" } }
                        order: { name: ASC }
                    ) { items { name } }
                    psf4: relatedpsf(
                        order: { name: ASC }
                    ) { items { name } }
                    psf5: relatedpsf { items { name } }
                    relatedsf { name }
                    relatedp { items { name } }
                } 
            }
        }
        """,
        "parent_item_with_children"
    )]
    [InlineData(
        """
        {
            sampleResponses(
                percentage: 10
                take: 5
                order: { name: ASC }
                where: { name: { neq: "some name" } }
            ) {
                items {
                    name
                    info
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
    [InlineData("""{ info }""", null, "info")]
    [InlineData("""{ message }""", null, "message")]
    [InlineData("""{ invalidRecord(obj: { text: "test" }) { text } }""", 1, "invalid_record")]
    [InlineData("""{ invalidRecordExt(obj: { text: "test" }) { text } }""", 1, "invalid_record_ext")]
    [InlineData("""{ sample(obj: null) { name } }""", null, "sample_null_no_errors")]
    [InlineData("""{ sample(obj: { name: "" }) { name } }""", 1, "sample_blank_name_required")]
    [InlineData("""{ sample(obj: { name: "Jane" }) { name } }""", null, "sample_no_errors")]
    [InlineData("""{ sample(obj: { name: "ab" }) { name } }""", 1, "sample_min_length_3")]
    [InlineData("""{ sample(obj: { name: "empty-property-name" }) { name } }""", 1, "sample_empty-property-name_custom_validation")]
    [InlineData("""{ sample(obj: { name: "message-from-service" }) { name } }""", 1, "sample_message-from-service_custom_validation")]
    [InlineData("""{ sample(obj: { name: "multiple-property-names" }) { name } }""", 4, "sample_multiple-property-names_custom_validation")]
    [InlineData("""{ sample(obj: { name: "no-property-name" }) { name } }""", 1, "sample_no-property-name_custom_validation")]
    [InlineData("""{ sample(obj: { name: "null-error-message" }) { name } }""", 1, "sample_null-error-message_custom_validation")]
    [InlineData("""{ sample(obj: { name: null }) { name } }""", 1, "sample_required")]
    [InlineData("""{ sampleAlias:sample(obj: { name: "" }) { name } }""", 1, "sample_alias_blank_name_required")]
    [InlineData("""{ sampleIgnoreValidation(obj: null) { name } }""", null, "sampleIgnoreValidation_no_errors")]
    [InlineData("""{ sampleNonNull(obj: null) { name } }""", 1, "sampleNonNull_required")]
    [InlineData("""{ sampleNonNull(obj: { name: "ab" }) { name } }""", 1, "sampleNonNull_min_length_3")]
    [InlineData("""{ sampleWithService(obj: { name: "Jane" }) { name } }""", null, "sampleWithService_no_errors")]
    [InlineData("""{ text(txt: "abc") }""", 1, "text_min_length_5")]
    [InlineData("""{ text(txt: "abcdefg") }""", null, "text_no_errors")]
    [InlineData("""{ textAlias:text(txt: "abc") }""", 1, "text_alias_min_length_5")]
    [InlineData("""{ textIgnoreValidation(txt: "a") }""", null, "textIgnoreValidation_no_errors")]
    [InlineData("""mutation { setText(txt: "abc") }""", 1, "setText_min_length_5")]
    [InlineData("""mutation { setSample(obj: { name: "" }) { name } }""", 1, "setSample_blank_name_required")]
    [InlineData("""mutation { setSampleRecord(obj: { name: "" }) { name } }""", 1, "setSampleRecord_blank_name_required")]
    [InlineData(
        """mutation { setSampleRecordWithSynthesizedProperty(obj: { name: "" }) { name } }""",
        1,
        "setSampleRecordWithSynthesizedProperty_blank_name_required"
    )]
    [InlineData(
        """mutation { setSampleRecordWithClassLevelValidationAttribute(obj: { name: null info: null }) { name info } }""",
        1,
        "setSampleRecordWithClassLevelValidationAttribute_top_level_validation_error"
    )]
    [InlineData(
        """mutation { setSampleRecordWithClassLevelValidationAttribute(obj: { name: "a" info: "info" }) { name info } }""",
        1,
        "setSampleRecordWithClassLevelValidationAttribute_invalid_name"
    )]
    [InlineData(
        """mutation { setSampleRecordWithClassLevelValidationAttribute(obj: { name: "Jane" info: "info" }) { name info } }""",
        null,
        "setSampleRecordWithClassLevelValidationAttribute_no_errors"
    )]
    [InlineData("""mutation { setSampleRecordWithParameterValidationAttribute(obj: { name: "" info: "" }) { name info } }""",
        1,
        "setSampleRecordWithParameterValidationAttribute_top_level_validation_error"
    )]
    [InlineData(
        """mutation { setSampleRecordWithParameterValidationAttribute(obj: { name: "a" info: "info"  }) { name info } }""",
        1,
        "setSampleRecordWithParameterValidationAttribute_invalid_name"
    )]
    [InlineData(
        """mutation { setSampleRecordWithParameterValidationAttribute(obj: { name: "Jane" info: "info"  }) { name info } }""",
        null,
        "setSampleRecordWithParameterValidationAttribute_no_errors"
    )]
    [InlineData(
        """
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
    [InlineData(
        """
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
}
