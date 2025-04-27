using DataAnnotatedModelValidations.Attributes;
using HotChocolate.Language;

namespace DataAnnotatedModelValidations.Tests.Pipeline;

public partial class PipelineExecutionTests
{
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
                Name = "Jane"
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

            return new SampleResponse[]
                {
                    new()
                    {
                        Name = "John Doe",
                        Info = "john_doe",
                        NumberOfPets = 2,
                        Age = 30
                    },
                    new()
                    {
                        Name = "Jane Doe",
                        NumberOfPets = 1,
                        Age = 25
                    },
                    new()
                    {
                        Name = "Bob Smith",
                        Info = "bob_smith",
                        NumberOfPets = 0
                    },
                    new()
                    {
                        Name = "Alice Johnson"
                    },
                    new()
                    {
                        Info = "unknown_user"
                    }
                }
                .AsQueryable()
                .With(queryContext)
                .ToList();
        }

        public string? GetText([StringLength(100, MinimumLength = 5)] string? txt) => txt;

        public string? GetTextIgnoreValidation([IgnoreModelValidation] [StringLength(100, MinimumLength = 5)] string? txt) => txt;

        public Sample? GetSample(Sample? obj) => obj;

        public Sample GetSampleNonNull(Sample obj) => obj;

        public Sample? GetSampleIgnoreValidation([IgnoreModelValidation] Sample? obj) => obj;

        public Sample? GetSampleWithService(Sample? obj, [Service] MockService service) =>
            service.Get(obj?.Name);

        public InvalidRecord GetInvalidRecord(InvalidRecord obj) => obj;
    }

    [ExtendObjectType(OperationTypeNames.Query)]
    public class QueryExtension
    {
        public InvalidRecord GetInvalidRecordExt([Parent] Query parent, InvalidRecord obj) => parent.GetInvalidRecord(obj);
    }

    public class Mutation
    {
        public string? SetText([StringLength(100, MinimumLength = 5)] string? txt) => txt;

        public Sample? SetSample(Sample? obj) => obj;

        public SampleRecord? SetSampleRecord(SampleRecord? obj) => obj;

        public SampleRecordWithSynthesizedProperty? SetSampleRecordWithSynthesizedProperty(SampleRecordWithSynthesizedProperty? obj) => obj;

        public SampleRecordWithClassLevelValidationAttribute? SetSampleRecordWithClassLevelValidationAttribute(
            SampleRecordWithClassLevelValidationAttribute? obj
        ) => obj;

        public SampleRecordWithParameterValidationAttribute? SetSampleRecordWithParameterValidationAttribute(
            [AllStringPropertiesMustBeFilled] SampleRecordWithParameterValidationAttribute? obj
        ) => obj;

        public NestedParent SetNestedParent(NestedParent obj) => obj;
    }

    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class MutationExtensionByName
    {
        public NestedParent SetNestedParentExtByName([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
    }

    [ExtendObjectType(OperationType.Mutation)]
    public class MutationExtensionByOperationType
    {
        public NestedParent SetNestedParentExtByOpType([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
    }

    [ExtendObjectType(typeof(Mutation))]
    public class MutationExtensionByType
    {
        public NestedParent SetNestedParentExtByType([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
    }

    [ExtendObjectType<Mutation>]
    public class MutationExtensionGeneric
    {
        public NestedParent SetNestedParentExtGeneric([Parent] Mutation parent, NestedParent obj) => parent.SetNestedParent(obj);
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
                Name = "Jane"
            },
            new()
            {
                Name = "Bob"
            }
        }.AsQueryable();

        [UseSorting]
        [UseFiltering]
        public IQueryable<Sample> Relatedsf => new Sample[]
        {
            new()
            {
                Name = "Jane"
            },
            new()
            {
                Name = "Bob"
            }
        }.AsQueryable();

        [UseOffsetPaging]
        [UseFiltering]
        public IQueryable<Sample> Relatedp => new Sample[]
        {
            new()
            {
                Name = "Jane"
            },
            new()
            {
                Name = "Bob"
            }
        }.AsQueryable();
    }
}
